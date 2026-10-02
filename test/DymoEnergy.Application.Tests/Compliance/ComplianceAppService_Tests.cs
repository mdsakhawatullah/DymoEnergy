using System;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.ProjectPlanning;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Compliance;

public abstract class ComplianceAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IComplianceAppService _service;
    private readonly IProjectPlanningAppService _projects;

    protected ComplianceAppService_Tests()
    {
        _service  = GetRequiredService<IComplianceAppService>();
        _projects = GetRequiredService<IProjectPlanningAppService>();
    }

    [Fact]
    public async Task Should_Seed_Editable_Starter_Content_Once()
    {
        var page = await _service.GetPageAsync();

        page.Licences.Count.ShouldBe(9);
        page.Licences.Where(l => l.Tone == "none").Select(l => l.Name).ShouldBe(new[] { "VAT registration (BIN)", "TIN certificate" });
        page.Licences.Where(l => l.HasExpiry).All(l => l.Tone == "unset").ShouldBeTrue();
        page.Filings.Count.ShouldBe(4);
        page.Items.Count(i => i.Kind == ComplianceItemKind.DocType).ShouldBe(6);
        page.Items.Count(i => i.Kind == ComplianceItemKind.Badge).ShouldBe(8);
        page.Items.Count(i => i.Kind == ComplianceItemKind.Reminder).ShouldBe(3);
        page.Setting.Labels.First(l => l.Key == "page.title").Value.ShouldBe("Compliance & documents");
        page.Setting.ImportRequiredDocs.ShouldBe("LC, Invoice, Packing list, Bill of entry, Duty receipt");

        (await _service.GetPageAsync()).Licences.Count.ShouldBe(9);
    }

    [Fact]
    public async Task Should_Customise_Labels_And_Settings()
    {
        var setting = await _service.UpdateSettingAsync(new UpdateComplianceSettingDto
        {
            AccentColor = "#7C3AED", ExpiringDays = 45, ImportRequiredDocs = "LC, Invoice",
            Labels = new() { ["page.title"] = "Paperwork", ["tab.filings"] = "Taxes", ["status.good"] = "Yes" },
        });

        setting.AccentColor.ShouldBe("#7C3AED");
        setting.ExpiringDays.ShouldBe(45);
        setting.Labels.First(l => l.Key == "page.title").Value.ShouldBe("Paperwork");
        setting.Labels.First(l => l.Key == "tab.filings").Value.ShouldBe("Taxes");
        // Untouched labels keep the built-in text.
        setting.Labels.First(l => l.Key == "tab.licences").Value.ShouldBe("Licences");

        // A label the user blanks out falls back to the default instead of showing nothing.
        var blanked = await _service.UpdateSettingAsync(new UpdateComplianceSettingDto
        {
            AccentColor = "#7C3AED", ExpiringDays = 45, Labels = new() { ["page.title"] = "  " },
        });
        blanked.Labels.First(l => l.Key == "page.title").Value.ShouldBe("Compliance & documents");
    }

    [Fact]
    public async Task Should_Classify_Licences_And_Build_The_Alert()
    {
        var today = DateTime.Today;
        var seededPage = await _service.GetPageAsync();
        foreach (var f in seededPage.Filings)   // keep the seeded monthly filings out of this licence-focused check
            await _service.UpdateFilingStatusAsync(f.Id, new UpdateComplianceFilingStatusDto { Status = ComplianceFilingStatus.Filed });
        var seeded = seededPage.Licences;
        await _service.UpdateLicenceAsync(seeded.First(l => l.Name == "Bank solvency certificate").Id, new CreateUpdateComplianceLicenceDto { Name = "Bank solvency certificate", ValidUntil = today.AddDays(-14), IssuedOn = today.AddYears(-1) });
        await _service.UpdateLicenceAsync(seeded.First(l => l.Name == "Fire licence").Id, new CreateUpdateComplianceLicenceDto { Name = "Fire licence", ValidUntil = today.AddDays(11), IssuedOn = today.AddDays(-354) });
        await _service.CreateLicenceAsync(new CreateUpdateComplianceLicenceDto { Name = "Trade licence copy", ValidUntil = today.AddDays(92), IssuedOn = today.AddDays(-273) });

        var page = await _service.GetPageAsync();

        var bank = page.Licences.First(l => l.Name == "Bank solvency certificate");
        bank.Tone.ShouldBe("expired");
        bank.StatusText.ShouldBe("14 days overdue");
        var fire = page.Licences.First(l => l.Name == "Fire licence");
        fire.Tone.ShouldBe("soon");
        fire.StatusText.ShouldBe("11 days left");
        fire.Fraction.ShouldBeInRange(0.02, 0.05);
        page.Licences.First(l => l.Name == "Trade licence copy").Tone.ShouldBe("ok");

        page.Summary.StatusKey.ShouldBe("almost");
        page.Summary.StatusDetail.ShouldBe("1 expired, 1 due within a month");
        page.Summary.Licences.Note.ShouldBe("1 expired");
        page.Summary.Licences.Tone.ShouldBe("red");
        page.Summary.Licences.Badge.ShouldBe(2);
        page.Summary.Alert.ShouldNotBeNull();
        page.Summary.Alert!.Text.ShouldBe("Bank solvency certificate expired 14 days ago, and the fire licence has 11 days left. Both are asked for in tenders.");
        page.Summary.Alert.LicenceId.ShouldBe(bank.Id);
    }

    [Fact]
    public async Task Should_Track_Filings_History_And_Status()
    {
        var today = DateTime.Today;
        var filings = (await _service.GetPageAsync()).Filings;
        filings.ShouldNotBeEmpty();

        var vat = await _service.CreateFilingAsync(new CreateUpdateComplianceFilingDto
        {
            Title = "Quarterly return", DueDate = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1),
        });
        vat.Tone.ShouldNotBe("done");
        (await _service.UpdateFilingStatusAsync(vat.Id, new UpdateComplianceFilingStatusDto { Status = ComplianceFilingStatus.Filed })).Tone.ShouldBe("done");

        for (var i = 1; i <= 3; i++)
            await _service.CreateItemAsync(new CreateUpdateComplianceListItemDto
            {
                Kind = ComplianceItemKind.FilingRecord, Title = "M" + i, Number = 2026, Extra = i == 2 ? "late" : "filed",
                Description = i == 2 ? "18 Jul · 3 days late" : "on time",
            });

        var stats = (await _service.GetPageAsync()).FilingStats;
        stats.OnTimeTotal.ShouldBe(3);
        stats.OnTime.ShouldBe(2);
        stats.LateCount.ShouldBe(1);
        stats.LateTitle.ShouldBe("M2 2026");
        stats.LateDetail.ShouldBe("18 Jul · 3 days late");
    }

    [Fact]
    public async Task Should_Flag_Certificates_That_Need_Attention()
    {
        var today = DateTime.Today;
        await _service.CreateCertificateAsync(new CreateUpdateComplianceCertificateDto
        {
            ProductName = "Mono PERC Panel 550W", Category = "Solar panels", Supplier = "[Supplier A]",
            Badges = new() { "IEC 61215", "Factory" }, TestReport = "Lab report 2025", ValidUntil = today.AddDays(190),
        });
        await _service.CreateCertificateAsync(new CreateUpdateComplianceCertificateDto { ProductName = "Tubular Battery 200Ah", Badges = new() { "Factory" } });
        var soon = await _service.CreateCertificateAsync(new CreateUpdateComplianceCertificateDto { ProductName = "MPPT 60A", TestReport = "Lab report 2024", ValidUntil = today.AddDays(25) });
        await _service.AddFileAsync(new AddComplianceFileDto { OwnerKind = ComplianceFileOwner.Certificate, OwnerId = soon.Id, FileName = "a.pdf", Url = "https://x/a.pdf", SizeBytes = 10 });
        await _service.AddFileAsync(new AddComplianceFileDto { OwnerKind = ComplianceFileOwner.Certificate, OwnerId = soon.Id, FileName = "b.pdf", Url = "https://x/b.pdf", SizeBytes = 10 });

        var certs = (await _service.GetPageAsync()).Certificates;
        certs.First(c => c.ProductName.StartsWith("Mono")).ValidText.ShouldBe("6 months");
        certs.First(c => c.ProductName.StartsWith("Mono")).Badges.ShouldBe(new[] { "IEC 61215", "Factory" });
        var missing = certs.First(c => c.ProductName.StartsWith("Tubular"));
        missing.Tone.ShouldBe("missing");
        missing.ValidText.ShouldBe("Not on file");
        var mppt = certs.First(c => c.ProductName == "MPPT 60A");
        mppt.ValidText.ShouldBe("25 days left");
        mppt.FileCount.ShouldBe(2);

        var summary = (await _service.GetPageAsync()).Summary.Certificates;
        summary.Total.ShouldBe(3);
        summary.Done.ShouldBe(1);
        summary.Note.ShouldBe("2 need attention");

        // "Replace" swaps the earlier files for the new one.
        await _service.AddFileAsync(new AddComplianceFileDto { OwnerKind = ComplianceFileOwner.Certificate, OwnerId = soon.Id, FileName = "c.pdf", Url = "https://x/c.pdf", ReplaceExisting = true });
        (await _service.GetPageAsync()).Certificates.First(c => c.Id == soon.Id).FileCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Build_Project_Document_Packs()
    {
        var stages = await _projects.GetStagesAsync();
        var running = await _projects.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Customer A" });
        var finished = await _projects.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Customer X", StageId = stages.Last().Id });

        var page = await _service.GetPageAsync();
        var docTypes = page.Items.Where(i => i.Kind == ComplianceItemKind.DocType).ToList();
        docTypes.Count.ShouldBe(6);

        foreach (var t in docTypes)
            await _service.SetProjectDocumentAsync(new SetComplianceProjectDocumentDto { ProjectId = finished.Id, DocTypeId = t.Id, Status = ComplianceDocStatus.Done });
        await _service.SetProjectDocumentAsync(new SetComplianceProjectDocumentDto { ProjectId = running.Id, DocTypeId = docTypes[0].Id, Status = ComplianceDocStatus.Done });
        await _service.SetProjectDocumentAsync(new SetComplianceProjectDocumentDto { ProjectId = running.Id, DocTypeId = docTypes[2].Id, Status = ComplianceDocStatus.InProgress });

        page = await _service.GetPageAsync();
        page.Packs.First(p => p.ProjectId == finished.Id).DonePercent.ShouldBe(100);
        var pack = page.Packs.First(p => p.ProjectId == running.Id);
        pack.DonePercent.ShouldBe(17);
        pack.Cells[2].Status.ShouldBe(ComplianceDocStatus.InProgress);
        page.Summary.Packs.Done.ShouldBe(1);
        page.Summary.Packs.Note.ShouldBe("1 incomplete");
        page.Summary.Packs.Badge.ShouldBe(0);

        // Setting a cell back to Pending clears it; deleting a column removes its cells.
        await _service.SetProjectDocumentAsync(new SetComplianceProjectDocumentDto { ProjectId = running.Id, DocTypeId = docTypes[0].Id, Status = ComplianceDocStatus.Pending });
        await _service.DeleteItemAsync(docTypes[5].Id);
        page = await _service.GetPageAsync();
        page.Packs.First(p => p.ProjectId == running.Id).Cells.Count.ShouldBe(5);
        page.Packs.First(p => p.ProjectId == running.Id).DonePercent.ShouldBe(0);
        page.Packs.First(p => p.ProjectId == finished.Id).DonePercent.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Edit_Lists_Without_Changing_Their_Kind()
    {
        var badge = await _service.CreateItemAsync(new CreateUpdateComplianceListItemDto { Kind = ComplianceItemKind.Badge, Title = "IEC 61853", Description = "Energy rating", Color = "#0B5A34" });
        badge.Order.ShouldBe(9);

        var updated = await _service.UpdateItemAsync(badge.Id, new CreateUpdateComplianceListItemDto { Kind = ComplianceItemKind.Reminder, Title = "IEC 61853-1", Description = "Rating" });
        updated.Kind.ShouldBe(ComplianceItemKind.Badge);
        updated.Title.ShouldBe("IEC 61853-1");

        var reminder = (await _service.GetPageAsync()).Items.First(i => i.Kind == ComplianceItemKind.Reminder && !i.Flag);
        var toggled = await _service.UpdateItemAsync(reminder.Id, new CreateUpdateComplianceListItemDto { Kind = ComplianceItemKind.Reminder, Title = reminder.Title, Description = reminder.Description, Flag = true });
        toggled.Flag.ShouldBeTrue();
    }
}
