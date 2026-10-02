using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.ProjectPlanning;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Compliance;

[Authorize(DymoEnergyPermissions.Compliance.Default)]
public class ComplianceAppService : ApplicationService, IComplianceAppService
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    private readonly IRepository<ComplianceSetting, int>         _settings;
    private readonly IRepository<ComplianceLicence, int>         _licences;
    private readonly IRepository<ComplianceFiling, int>          _filings;
    private readonly IRepository<ComplianceCertificate, int>     _certificates;
    private readonly IRepository<ComplianceListItem, int>        _items;
    private readonly IRepository<ComplianceProjectDocument, int> _projectDocs;
    private readonly IRepository<ComplianceFile, int>            _files;
    private readonly IRepository<Project, int>                   _projects;
    private readonly IRepository<ProjectStage, int>              _stages;

    public ComplianceAppService(
        IRepository<ComplianceSetting, int>         settings,
        IRepository<ComplianceLicence, int>         licences,
        IRepository<ComplianceFiling, int>          filings,
        IRepository<ComplianceCertificate, int>     certificates,
        IRepository<ComplianceListItem, int>        items,
        IRepository<ComplianceProjectDocument, int> projectDocs,
        IRepository<ComplianceFile, int>            files,
        IRepository<Project, int>                   projects,
        IRepository<ProjectStage, int>              stages)
    {
        _settings = settings; _licences = licences; _filings = filings; _certificates = certificates;
        _items = items; _projectDocs = projectDocs; _files = files; _projects = projects; _stages = stages;
    }

    // ══ PAGE ═════════════════════════════════════════════════════════════════

    public async Task<CompliancePageDto> GetPageAsync()
    {
        await EnsureDefaultsAsync();

        var setting = await LoadSettingAsync();
        var labels  = ReadLabels(setting);
        var today   = Clock.Now.Date;
        var soon    = setting.ExpiringDays;

        var files     = (await _files.GetListAsync()).OrderBy(f => f.Id).ToList();
        var items     = (await _items.GetListAsync()).OrderBy(i => i.Kind).ThenBy(i => i.Order).ThenBy(i => i.Id).ToList();
        var licences  = (await _licences.GetListAsync()).OrderBy(l => l.Order).ThenBy(l => l.Id)
                            .Select(l => MapLicence(l, today, soon)).ToList();
        var filings   = (await _filings.GetListAsync()).OrderBy(f => f.DueDate).ThenBy(f => f.Order)
                            .Select(f => MapFiling(f, today, soon)).ToList();
        var certs     = (await _certificates.GetListAsync()).OrderBy(c => c.ProductName)
                            .Select(c => MapCertificate(c, today, soon, files.Count(f => f.OwnerKind == ComplianceFileOwner.Certificate && f.OwnerId == c.Id)))
                            .ToList();

        var docTypes = items.Where(i => i.Kind == ComplianceItemKind.DocType).ToList();
        var packs    = await BuildPacksAsync(docTypes);

        var page = new CompliancePageDto
        {
            Setting      = MapSetting(setting, labels),
            Licences     = licences,
            Filings      = filings,
            FilingStats  = BuildFilingStats(filings, items, today),
            Certificates = certs,
            Items        = items.Select(MapItem).ToList(),
            Packs        = packs,
            Files        = files.Select(MapFile).ToList(),
        };
        page.Summary = BuildSummary(page, labels, today);
        return page;
    }

    private async Task<List<ComplianceProjectPackDto>> BuildPacksAsync(List<ComplianceListItem> docTypes)
    {
        var projects = (await _projects.GetListAsync()).OrderByDescending(p => p.Id).Take(100).ToList();
        var stages   = (await _stages.GetListAsync()).ToDictionary(s => s.Id);
        var ids      = projects.Select(p => p.Id).ToList();
        var docs     = ids.Count == 0
            ? new List<ComplianceProjectDocument>()
            : await _projectDocs.GetListAsync(d => ids.Contains(d.ProjectId));

        return projects.Select(p =>
        {
            stages.TryGetValue(p.StageId, out var stage);
            var cells = docTypes.Select(t => new ComplianceDocCellDto
            {
                DocTypeId = t.Id,
                Status    = docs.FirstOrDefault(d => d.ProjectId == p.Id && d.DocTypeId == t.Id)?.Status ?? ComplianceDocStatus.Pending,
            }).ToList();
            var done = cells.Count(c => c.Status == ComplianceDocStatus.Done);
            return new ComplianceProjectPackDto
            {
                ProjectId    = p.Id,
                Code         = p.Code,
                Name         = p.CustomerName,
                StageName    = stage?.Name ?? string.Empty,
                StageColor   = stage?.Color ?? "#6B7280",
                IsHandedOver = stage?.IsFinal == true,
                DonePercent  = cells.Count == 0 ? 0 : (int)Math.Round(done * 100.0 / cells.Count),
                Cells        = cells,
            };
        }).ToList();
    }

    private static ComplianceFilingStatsDto BuildFilingStats(List<ComplianceFilingDto> filings, List<ComplianceListItem> items, DateTime today)
    {
        var month   = filings.Where(f => f.DueDate.Year == today.Year && f.DueDate.Month == today.Month).ToList();
        var pending = month.Where(f => f.Tone != "done").ToList();
        var next    = pending.Where(f => f.DueInDays >= 0).Select(f => (int?)f.DueInDays).OrderBy(d => d).FirstOrDefault();

        var records = items.Where(i => i.Kind == ComplianceItemKind.FilingRecord)
            .OrderByDescending(i => i.Order).Take(12).ToList();
        var late = records.Where(r => string.Equals(r.Extra, "late", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Order).ToList();

        return new ComplianceFilingStatsDto
        {
            DueThisMonth = pending.Count,
            NextDueDays  = next,
            OnTime       = records.Count(r => string.Equals(r.Extra, "filed", StringComparison.OrdinalIgnoreCase)),
            OnTimeTotal  = records.Count,
            LateCount    = late.Count,
            LateTitle    = late.Count == 0 ? null : late[0].Title + (late[0].Number.HasValue ? " " + late[0].Number : ""),
            LateDetail   = late.Count == 0 ? null : late[0].Description,
        };
    }

    private static ComplianceSummaryDto BuildSummary(CompliancePageDto page, Dictionary<string, string> labels, DateTime today)
    {
        var lic  = page.Licences;
        var cert = page.Certificates;
        var fil  = page.Filings.Where(f => f.DueDate.Year == today.Year && f.DueDate.Month == today.Month).ToList();

        var licExpired = lic.Count(l => l.Tone == "expired");
        var licSoon    = lic.Count(l => l.Tone == "soon");
        var licUnset   = lic.Count(l => l.Tone == "unset");
        var certExpired = cert.Count(c => c.Tone == "expired");
        var certSoon    = cert.Count(c => c.Tone == "soon");
        var certAttn    = cert.Count(c => c.NeedsAttention);
        var filOverdue  = page.Filings.Count(f => f.Tone == "overdue");
        var filSoon     = page.Filings.Count(f => f.Tone == "soon");

        var expired = licExpired + certExpired + filOverdue;
        var dueSoon = licSoon + certSoon + filSoon;

        var parts = new List<string>();
        if (expired > 0) parts.Add($"{expired} expired");
        if (dueSoon > 0) parts.Add($"{dueSoon} due within a month");

        var summary = new ComplianceSummaryDto
        {
            StatusKey    = expired + dueSoon == 0 ? "good" : expired >= 3 ? "bad" : "almost",
            StatusDetail = parts.Count == 0 ? "nothing expired or due soon" : string.Join(", ", parts),
        };

        summary.Licences = new ComplianceSectionSummaryDto
        {
            Done  = lic.Count(l => l.Tone is "ok" or "soon" or "none"),
            Total = lic.Count,
            Badge = licExpired + licSoon,
            Note  = licExpired > 0 ? $"{licExpired} expired" : licSoon > 0 ? $"{licSoon} due soon" : licUnset > 0 ? $"{licUnset} dates missing" : "all valid",
            Tone  = licExpired > 0 ? "red" : licSoon > 0 || licUnset > 0 ? "amber" : "green",
        };

        var nextDue = fil.Where(f => f.Tone != "done" && f.DueInDays >= 0).Select(f => (int?)f.DueInDays).OrderBy(d => d).FirstOrDefault();
        summary.Filings = new ComplianceSectionSummaryDto
        {
            Done  = fil.Count(f => f.Tone == "done"),
            Total = fil.Count,
            Badge = filOverdue + filSoon,
            Note  = filOverdue > 0 ? $"{filOverdue} overdue" : nextDue.HasValue ? $"next due in {Plural(nextDue.Value, "day")}" : "all filed",
            Tone  = filOverdue > 0 ? "red" : nextDue.HasValue ? "amber" : "green",
        };

        summary.Certificates = new ComplianceSectionSummaryDto
        {
            Done  = cert.Count - certAttn,
            Total = cert.Count,
            Badge = certAttn,
            Note  = certAttn > 0 ? $"{certAttn} need attention" : "all in order",
            Tone  = certExpired > 0 || cert.Any(c => c.Tone == "missing") ? "red" : certAttn > 0 ? "amber" : "green",
        };

        var incomplete = page.Packs.Count(p => p.DonePercent < 100);
        summary.Packs = new ComplianceSectionSummaryDto
        {
            Done  = page.Packs.Count - incomplete,
            Total = page.Packs.Count,
            Badge = page.Packs.Count(p => p.IsHandedOver && p.DonePercent < 100),
            Note  = incomplete > 0 ? $"{incomplete} incomplete" : "all complete",
            Tone  = incomplete > 0 ? "amber" : "green",
        };

        // Red banner: the licences that need action first (expired, then soonest to expire).
        var urgent = lic.Where(l => l.Tone is "expired" or "soon").OrderBy(l => l.DaysLeft).Take(3).ToList();
        if (urgent.Count > 0)
        {
            var phrases = urgent.Select((l, i) =>
            {
                var name = i == 0 ? l.Name : "the " + LowerFirst(l.Name);
                return l.Tone == "expired"
                    ? $"{name} expired {Plural(-l.DaysLeft!.Value, "day")} ago"
                    : $"{name} has {Plural(l.DaysLeft!.Value, "day")} left";
            }).ToList();

            var text = phrases.Count == 1
                ? phrases[0]
                : string.Join(", ", phrases.Take(phrases.Count - 1)) + ", and " + phrases.Last();
            labels.TryGetValue("alert.footnote", out var foot);
            summary.Alert = new ComplianceAlertDto
            {
                Text      = text + "." + (string.IsNullOrWhiteSpace(foot) ? "" : " " + foot),
                LicenceId = urgent[0].Id,
            };
        }

        return summary;
    }

    // ══ SETTINGS ═════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceSettingDto> UpdateSettingAsync(UpdateComplianceSettingDto input)
    {
        await EnsureDefaultsAsync();
        var s = await LoadSettingAsync();
        s.AccentColor        = input.AccentColor;
        s.ExpiringDays       = input.ExpiringDays;
        s.ImportRequiredDocs = Clean(input.ImportRequiredDocs);

        // Keep only labels that differ from the built-in text, so default changes still reach untouched labels.
        var overrides = new Dictionary<string, string>();
        foreach (var def in ComplianceDefaults.Labels)
        {
            if (input.Labels.TryGetValue(def.Key, out var v) && v != def.Default && !(string.IsNullOrWhiteSpace(v) && def.Default.Length > 0))
                overrides[def.Key] = v;
        }
        s.LabelsJson = JsonSerializer.Serialize(overrides);

        await _settings.UpdateAsync(s, autoSave: true);
        return MapSetting(s, ReadLabels(s));
    }

    // ══ LICENCES ═════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Create)]
    public async Task<ComplianceLicenceDto> CreateLicenceAsync(CreateUpdateComplianceLicenceDto input)
    {
        await EnsureDefaultsAsync();
        var l = new ComplianceLicence();
        ApplyLicence(l, input);
        if (input.Order <= 0)
            l.Order = (await _licences.GetListAsync()).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
        await _licences.InsertAsync(l, autoSave: true);
        return await LicenceDtoAsync(l);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceLicenceDto> UpdateLicenceAsync(int id, CreateUpdateComplianceLicenceDto input)
    {
        var l = await _licences.GetAsync(id);
        ApplyLicence(l, input);
        await _licences.UpdateAsync(l, autoSave: true);
        return await LicenceDtoAsync(l);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Delete)]
    public async Task DeleteLicenceAsync(int id)
    {
        await _files.DeleteAsync(f => f.OwnerKind == ComplianceFileOwner.Licence && f.OwnerId == id);
        await _licences.DeleteAsync(id, autoSave: true);
    }

    private async Task<ComplianceLicenceDto> LicenceDtoAsync(ComplianceLicence l)
    {
        var s = await LoadSettingAsync();
        return MapLicence(l, Clock.Now.Date, s.ExpiringDays);
    }

    // ══ FILINGS ══════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Create)]
    public async Task<ComplianceFilingDto> CreateFilingAsync(CreateUpdateComplianceFilingDto input)
    {
        var f = new ComplianceFiling();
        ApplyFiling(f, input);
        await _filings.InsertAsync(f, autoSave: true);
        return await FilingDtoAsync(f);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceFilingDto> UpdateFilingAsync(int id, CreateUpdateComplianceFilingDto input)
    {
        var f = await _filings.GetAsync(id);
        ApplyFiling(f, input);
        await _filings.UpdateAsync(f, autoSave: true);
        return await FilingDtoAsync(f);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceFilingDto> UpdateFilingStatusAsync(int id, UpdateComplianceFilingStatusDto input)
    {
        var f = await _filings.GetAsync(id);
        f.Status = input.Status;
        await _filings.UpdateAsync(f, autoSave: true);
        return await FilingDtoAsync(f);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Delete)]
    public async Task DeleteFilingAsync(int id)
    {
        await _filings.DeleteAsync(id, autoSave: true);
    }

    private async Task<ComplianceFilingDto> FilingDtoAsync(ComplianceFiling f)
    {
        var s = await LoadSettingAsync();
        return MapFiling(f, Clock.Now.Date, s.ExpiringDays);
    }

    // ══ CERTIFICATES ═════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Create)]
    public async Task<ComplianceCertificateDto> CreateCertificateAsync(CreateUpdateComplianceCertificateDto input)
    {
        var c = new ComplianceCertificate();
        ApplyCertificate(c, input);
        await _certificates.InsertAsync(c, autoSave: true);
        return await CertificateDtoAsync(c);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceCertificateDto> UpdateCertificateAsync(int id, CreateUpdateComplianceCertificateDto input)
    {
        var c = await _certificates.GetAsync(id);
        ApplyCertificate(c, input);
        await _certificates.UpdateAsync(c, autoSave: true);
        return await CertificateDtoAsync(c);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Delete)]
    public async Task DeleteCertificateAsync(int id)
    {
        await _files.DeleteAsync(f => f.OwnerKind == ComplianceFileOwner.Certificate && f.OwnerId == id);
        await _certificates.DeleteAsync(id, autoSave: true);
    }

    private async Task<ComplianceCertificateDto> CertificateDtoAsync(ComplianceCertificate c)
    {
        var s = await LoadSettingAsync();
        var count = await _files.CountAsync(f => f.OwnerKind == ComplianceFileOwner.Certificate && f.OwnerId == c.Id);
        return MapCertificate(c, Clock.Now.Date, s.ExpiringDays, count);
    }

    // ══ LIST ITEMS ═══════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceListItemDto> CreateItemAsync(CreateUpdateComplianceListItemDto input)
    {
        await EnsureDefaultsAsync();
        var item = new ComplianceListItem { Kind = input.Kind };
        ApplyItem(item, input);
        if (input.Order <= 0)
            item.Order = (await _items.GetListAsync(i => i.Kind == input.Kind)).Select(i => i.Order).DefaultIfEmpty(0).Max() + 1;
        await _items.InsertAsync(item, autoSave: true);
        return MapItem(item);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task<ComplianceListItemDto> UpdateItemAsync(int id, CreateUpdateComplianceListItemDto input)
    {
        var item = await _items.GetAsync(id);
        ApplyItem(item, input);   // Kind never changes after creation
        await _items.UpdateAsync(item, autoSave: true);
        return MapItem(item);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Delete)]
    public async Task DeleteItemAsync(int id)
    {
        var item = await _items.GetAsync(id);
        if (item.Kind == ComplianceItemKind.DocType)
            await _projectDocs.DeleteAsync(d => d.DocTypeId == id);
        if (item.Kind is ComplianceItemKind.Template or ComplianceItemKind.ImportPaper)
        {
            var owner = item.Kind == ComplianceItemKind.Template ? ComplianceFileOwner.Template : ComplianceFileOwner.ImportPaper;
            await _files.DeleteAsync(f => f.OwnerKind == owner && f.OwnerId == id);
        }
        await _items.DeleteAsync(id, autoSave: true);
    }

    // ══ PROJECT PACKS ════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Edit)]
    public async Task SetProjectDocumentAsync(SetComplianceProjectDocumentDto input)
    {
        var existing = await _projectDocs.FirstOrDefaultAsync(d => d.ProjectId == input.ProjectId && d.DocTypeId == input.DocTypeId);
        if (input.Status == ComplianceDocStatus.Pending)
        {
            if (existing != null) await _projectDocs.DeleteAsync(existing, autoSave: true);
            return;
        }
        if (existing == null)
            await _projectDocs.InsertAsync(new ComplianceProjectDocument { ProjectId = input.ProjectId, DocTypeId = input.DocTypeId, Status = input.Status }, autoSave: true);
        else
        {
            existing.Status = input.Status;
            await _projectDocs.UpdateAsync(existing, autoSave: true);
        }
    }

    // ══ FILES ════════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Compliance.Create)]
    public async Task<ComplianceFileDto> AddFileAsync(AddComplianceFileDto input)
    {
        if (input.ReplaceExisting)
            await _files.DeleteAsync(f => f.OwnerKind == input.OwnerKind && f.OwnerId == input.OwnerId);

        var file = new ComplianceFile
        {
            OwnerKind = input.OwnerKind,
            OwnerId   = input.OwnerId,
            FileName  = input.FileName,
            Url       = input.Url,
            SizeBytes = input.SizeBytes,
        };
        await _files.InsertAsync(file, autoSave: true);
        return MapFile(file);
    }

    [Authorize(DymoEnergyPermissions.Compliance.Delete)]
    public async Task DeleteFileAsync(int id)
    {
        await _files.DeleteAsync(id, autoSave: true);
    }

    // ══ MAPPING ══════════════════════════════════════════════════════════════

    private static ComplianceLicenceDto MapLicence(ComplianceLicence l, DateTime today, int soonDays)
    {
        var dto = new ComplianceLicenceDto
        {
            Id = l.Id, Name = l.Name, Description = l.Description, Number = l.Number, IssuedBy = l.IssuedBy,
            Owner = l.Owner, IssuedOn = l.IssuedOn, ValidUntil = l.ValidUntil, HasExpiry = l.HasExpiry, Order = l.Order,
        };

        if (!l.HasExpiry)
        {
            dto.Tone = "none"; dto.StatusText = "No expiry"; dto.Fraction = 1;
        }
        else if (!l.ValidUntil.HasValue)
        {
            dto.Tone = "unset"; dto.StatusText = "Date missing"; dto.Fraction = 0;
        }
        else
        {
            var days  = (l.ValidUntil.Value.Date - today).Days;
            var start = (l.IssuedOn ?? l.ValidUntil.Value.AddYears(-1)).Date;
            var total = Math.Max(1, (l.ValidUntil.Value.Date - start).Days);
            dto.DaysLeft = days;
            dto.Fraction = Math.Clamp(days / (double)total, 0, 1);
            if (days < 0)             { dto.Tone = "expired"; dto.StatusText = $"{Plural(-days, "day")} overdue"; }
            else if (days <= soonDays) { dto.Tone = "soon";    dto.StatusText = $"{Plural(days, "day")} left"; }
            else                       { dto.Tone = "ok";      dto.StatusText = $"{Plural(days, "day")} left"; }
        }
        return dto;
    }

    private static ComplianceFilingDto MapFiling(ComplianceFiling f, DateTime today, int soonDays)
    {
        var days = (f.DueDate.Date - today).Days;
        var done = f.Status is ComplianceFilingStatus.Filed or ComplianceFilingStatus.UpToDate;
        return new ComplianceFilingDto
        {
            Id = f.Id, Title = f.Title, Detail = f.Detail, DueDate = f.DueDate, Status = f.Status,
            Owner = f.Owner, ActionLabel = f.ActionLabel, Order = f.Order,
            DueInDays = days,
            Tone = done ? "done" : days < 0 ? "overdue" : days <= Math.Min(soonDays, 14) ? "soon" : "ok",
        };
    }

    private static ComplianceCertificateDto MapCertificate(ComplianceCertificate c, DateTime today, int soonDays, int fileCount)
    {
        var dto = new ComplianceCertificateDto
        {
            Id = c.Id, ProductName = c.ProductName, Category = c.Category, Supplier = c.Supplier,
            Badges = SplitCsv(c.Badges), TestReport = c.TestReport, ValidUntil = c.ValidUntil, FileCount = fileCount,
        };

        if (string.IsNullOrWhiteSpace(c.TestReport) && !c.ValidUntil.HasValue)
        {
            dto.Tone = "missing"; dto.ValidText = "Not on file";
        }
        else if (!c.ValidUntil.HasValue)
        {
            dto.Tone = "ok"; dto.ValidText = "No expiry";
        }
        else
        {
            var days = (c.ValidUntil.Value.Date - today).Days;
            if (days < 0)              { dto.Tone = "expired"; dto.ValidText = $"Expired {Plural(-days, "day")} ago"; }
            else if (days <= soonDays) { dto.Tone = "soon";    dto.ValidText = $"{Plural(days, "day")} left"; }
            else if (days < 60)        { dto.Tone = "ok";      dto.ValidText = $"{Plural(days, "day")} left"; }
            else                       { dto.Tone = "ok";      dto.ValidText = Plural(days / 30, "month"); }
        }
        dto.NeedsAttention = dto.Tone is "missing" or "expired" or "soon";
        return dto;
    }

    private static ComplianceListItemDto MapItem(ComplianceListItem i) => new()
    {
        Id = i.Id, Kind = i.Kind, Title = i.Title, Description = i.Description, Extra = i.Extra,
        Color = i.Color, Number = i.Number, Flag = i.Flag, Order = i.Order,
    };

    private static ComplianceFileDto MapFile(ComplianceFile f) => new()
    {
        Id = f.Id, OwnerKind = f.OwnerKind, OwnerId = f.OwnerId, FileName = f.FileName, Url = f.Url,
        SizeBytes = f.SizeBytes, CreationTime = f.CreationTime,
    };

    private static ComplianceSettingDto MapSetting(ComplianceSetting s, Dictionary<string, string> labels) => new()
    {
        AccentColor        = s.AccentColor,
        ExpiringDays       = s.ExpiringDays,
        ImportRequiredDocs = s.ImportRequiredDocs,
        Labels = ComplianceDefaults.Labels.Select(d => new ComplianceLabelDto
        {
            Key = d.Key, Group = d.Group, Caption = d.Caption, Value = labels[d.Key],
        }).ToList(),
    };

    private static Dictionary<string, string> ReadLabels(ComplianceSetting s)
    {
        var map = ComplianceDefaults.Labels.ToDictionary(d => d.Key, d => d.Default);
        if (string.IsNullOrWhiteSpace(s.LabelsJson)) return map;
        try
        {
            var overrides = JsonSerializer.Deserialize<Dictionary<string, string>>(s.LabelsJson) ?? new();
            foreach (var (k, v) in overrides)
                if (map.ContainsKey(k)) map[k] = v;
        }
        catch (JsonException) { /* bad stored JSON: fall back to defaults */ }
        return map;
    }

    private static void ApplyLicence(ComplianceLicence l, CreateUpdateComplianceLicenceDto i)
    {
        l.Name = i.Name.Trim(); l.Description = Clean(i.Description); l.Number = Clean(i.Number);
        l.IssuedBy = Clean(i.IssuedBy); l.Owner = Clean(i.Owner);
        l.IssuedOn = i.IssuedOn?.Date; l.ValidUntil = i.HasExpiry ? i.ValidUntil?.Date : null;
        l.HasExpiry = i.HasExpiry;
        if (i.Order > 0) l.Order = i.Order;
    }

    private static void ApplyFiling(ComplianceFiling f, CreateUpdateComplianceFilingDto i)
    {
        f.Title = i.Title.Trim(); f.Detail = Clean(i.Detail); f.DueDate = i.DueDate.Date; f.Status = i.Status;
        f.Owner = Clean(i.Owner); f.ActionLabel = Clean(i.ActionLabel); f.Order = i.Order;
    }

    private static void ApplyCertificate(ComplianceCertificate c, CreateUpdateComplianceCertificateDto i)
    {
        c.ProductName = i.ProductName.Trim(); c.Category = Clean(i.Category); c.Supplier = Clean(i.Supplier);
        c.Badges = i.Badges.Count == 0 ? null : string.Join(", ", i.Badges.Select(b => b.Replace(",", " ").Trim()).Where(b => b.Length > 0));
        c.TestReport = Clean(i.TestReport); c.ValidUntil = i.ValidUntil?.Date;
    }

    private static void ApplyItem(ComplianceListItem item, CreateUpdateComplianceListItemDto i)
    {
        item.Title = i.Title.Trim(); item.Description = Clean(i.Description); item.Extra = Clean(i.Extra);
        item.Color = Clean(i.Color); item.Number = i.Number; item.Flag = i.Flag;
        if (i.Order > 0) item.Order = i.Order;
    }

    // ══ SMALL HELPERS ════════════════════════════════════════════════════════

    private async Task<ComplianceSetting> LoadSettingAsync()
    {
        await EnsureDefaultsAsync();
        return (await _settings.GetListAsync()).OrderBy(s => s.Id).First();
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static List<string> SplitCsv(string? s) =>
        (s ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string Plural(int n, string unit) => n == 1 ? $"1 {unit}" : $"{n} {unit}s";

    /// <summary>"Fire licence" → "fire licence", but "VAT registration" stays as is.</summary>
    private static string LowerFirst(string s) =>
        s.Length > 1 && char.IsUpper(s[0]) && char.IsLower(s[1]) ? char.ToLowerInvariant(s[0]) + s[1..] : s;

    // ══ DEFAULTS ═════════════════════════════════════════════════════════════

    private async Task EnsureDefaultsAsync()
    {
        if (await _settings.GetCountAsync() > 0) return;

        await SeedLock.WaitAsync();
        try
        {
            if (await _settings.GetCountAsync() > 0) return;

            await _settings.InsertAsync(new ComplianceSetting
            {
                ImportRequiredDocs = string.Join(", ", ComplianceDefaults.ImportDocs),
                LabelsJson         = "{}",
            }, autoSave: true);

            if (await _licences.GetCountAsync() > 0) return;

            var order = 0;
            await _licences.InsertManyAsync(ComplianceDefaults.Licences.Select(s => new ComplianceLicence
            {
                Name = s.Name, Description = s.Description, IssuedBy = s.IssuedBy.Length == 0 ? null : s.IssuedBy,
                Owner = s.Owner, HasExpiry = s.HasExpiry, Order = ++order,
            }), autoSave: true);

            var today = Clock.Now.Date;
            var lastDay = DateTime.DaysInMonth(today.Year, today.Month);
            order = 0;
            await _filings.InsertManyAsync(ComplianceDefaults.Filings.Select(s => new ComplianceFiling
            {
                Title = s.Title, Detail = s.Detail, Owner = s.Owner, ActionLabel = s.Action, Order = ++order,
                DueDate = new DateTime(today.Year, today.Month, Math.Min(s.Day, lastDay)),
            }), autoSave: true);

            var kindOrder = new Dictionary<ComplianceItemKind, int>();
            await _items.InsertManyAsync(ComplianceDefaults.Items.Select(s =>
            {
                kindOrder[s.Kind] = kindOrder.GetValueOrDefault(s.Kind) + 1;
                return new ComplianceListItem
                {
                    Kind = s.Kind, Title = s.Title, Description = s.Description, Extra = s.Extra,
                    Color = s.Color, Number = s.Number, Flag = s.Flag, Order = kindOrder[s.Kind],
                };
            }), autoSave: true);
        }
        finally
        {
            SeedLock.Release();
        }
    }
}
