using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.QuoteRequests;

public abstract class QuoteRequestAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IQuoteRequestAppService _service;

    protected QuoteRequestAppService_Tests()
    {
        _service = GetRequiredService<IQuoteRequestAppService>();
    }

    [Fact]
    public async Task Should_Summarise_Filter_And_Update_Details()
    {
        await _service.CreateAsync(new CreateQuoteRequestDto
        {
            Name = "Rafiq Islam", Phone = "01813088367", Interest = "Residential rooftop",
            EstimatedSize = "~5 kW", Location = "Halishahar, Chattogram", Message = "Load-shedding every evening.",
        });
        await _service.CreateAsync(new CreateQuoteRequestDto { Name = "Sunrise Agro", Interest = "Solar irrigation pump" });
        await _service.CreateAsync(new CreateQuoteRequestDto { Name = "Meghna Textiles", Interest = "Commercial rooftop" });

        var list   = await _service.GetListDataAsync(new QuoteRequestFilterDto { Interest = "Residential rooftop" });
        var rafiq  = list.Items.ShouldHaveSingleItem();
        rafiq.Location.ShouldBe("Halishahar, Chattogram");

        await _service.UpdateStatusAsync(rafiq.Id, new UpdateQuoteRequestStatusDto { Status = QuoteRequestStatus.Quoted });
        var updated = await _service.UpdateDetailsAsync(rafiq.Id, new UpdateQuoteRequestDetailsDto
        {
            Interest = "Residential rooftop", EstimatedSize = "~5 kW", Location = "Halishahar, Chattogram", MonthlyBill = "BDT 6,000–8,000", RoofSite = "  ", AdminNote = "Site visit Sat",
        });
        updated.MonthlyBill.ShouldBe("BDT 6,000–8,000");
        updated.RoofSite.ShouldBeNull();
        updated.AdminNote.ShouldBe("Site visit Sat");
        updated.Status.ShouldBe(QuoteRequestStatus.Quoted);

        var summary = await _service.GetSummaryAsync(new QuoteRequestFilterDto());
        summary.QuotedCount.ShouldBeGreaterThanOrEqualTo(1);
        summary.AllCount.ShouldBe(summary.NewCount + summary.ContactedCount + summary.QuotedCount + summary.ClosedCount);
        summary.SystemTypes.ShouldContain("Solar irrigation pump");
        summary.SystemTypes.ShouldBe(summary.SystemTypes.OrderBy(t => t).ToList());

        (await _service.GetSummaryAsync(new QuoteRequestFilterDto { Filter = "Halishahar" })).AllCount.ShouldBe(1);
    }
}
