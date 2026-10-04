using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace DymoEnergy.Stock;

public partial class StockLedgerAppService
{
    /// <summary>How ABP names the permission provider that grants by role.</summary>
    private const string RoleProvider = "R";

    // ══ NEEDS A LOOK ═════════════════════════════════════════════════════════

    public async Task<LedgerNeedsLookDto> GetNeedsLookAsync()
    {
        var setting = await _writer.SettingAsync();
        var q = await _lines.GetQueryableAsync();
        var flagged = await AsyncExecuter.ToListAsync(q.Where(l => l.Flags != LedgerFlags.None).OrderByDescending(l => l.Id).Take(100));
        var reviewedIds = (await AsyncExecuter.ToListAsync((await _reviews.GetQueryableAsync()).Select(r => r.LineId))).ToHashSet();
        var open = flagged.Where(l => !reviewedIds.Contains(l.Id)).ToList();

        var dto = new LedgerNeedsLookDto
        {
            OpenCount = open.Count,
            OldestDays = open.Count == 0 ? null : (int)(Clock.Now.Date - open.Min(l => l.Time).Date).TotalDays,
            Setting = MapSetting(setting),
        };
        if (open.Count > 0)
        {
            var oldest = open.OrderBy(l => l.Id).First();
            dto.OldestText = Describe(oldest, setting).Title;
        }

        foreach (var line in open.OrderByDescending(l => Severity(l.Flags)).ThenByDescending(l => l.Id).Take(25))
        {
            var (title, text, action, label) = Describe(line, setting);
            dto.Lines.Add(new LedgerFlaggedDto
            {
                Line = MapLine(line, false), Title = title, Text = text, Action = action, ActionLabel = label,
                PairLineId = line.Flags.HasFlag(LedgerFlags.QuicklyReversed) ? await PairOfAsync(line, setting) : null,
            });
        }

        dto.Correctors = await HandCorrectorsAsync();
        var top = dto.Correctors.FirstOrDefault();
        if (top != null && dto.Correctors.Sum(c => c.Count) >= 5 && top.Percent >= 50)
            dto.CorrectorNote = $"{top.Name} posts {Math.Round(top.Percent / 10m)} in every 10 hand corrections. That may simply be the job, or it may be worth a count done by someone else.";
        return dto;
    }

    private static int Severity(LedgerFlags f) =>
        f.HasFlag(LedgerFlags.SerialNotReceived) || f.HasFlag(LedgerFlags.WouldGoBelowZero) ? 3
        : f.HasFlag(LedgerFlags.NotApproved) || f.HasFlag(LedgerFlags.QuicklyReversed) ? 2 : 1;

    private static (string Title, string Text, string Action, string Label) Describe(StockLedgerLine l, StockLedgerSetting s)
    {
        var what = $"{Math.Abs(l.Change)} × {l.ProductName}";
        if (l.Flags.HasFlag(LedgerFlags.SerialNotReceived))
            return ("Serial sold that was never received", $"A serial on {l.DocumentNumber} has no stock-in line behind it.", "investigate", "Investigate");
        if (l.Flags.HasFlag(LedgerFlags.WouldGoBelowZero))
            return ("Stock would have gone below zero", $"{what} was blocked because the count was already 0. Someone is selling stock the system has not received.", "open", "Open line");
        if (l.Flags.HasFlag(LedgerFlags.QuicklyReversed))
            return ($"Correction reversed within {s.QuicklyReversedMinutes} minutes", $"{what} changed, then put back. Worth asking what happened.", "both-lines", "See both lines");
        if (l.Flags.HasFlag(LedgerFlags.NotApproved))
            return ("Big write-off with nobody approving", $"{what} is over {Money(s.LargeValueOver)} and the two-person rule was not met.", "open", "Open line");
        if (l.Flags.HasFlag(LedgerFlags.NoPhoto))
            return ("Correction with no photo attached", $"{what} written off as a count difference. The rules ask for a photo.", "photo", "Ask for photo");
        if (l.Flags.HasFlag(LedgerFlags.NoReason))
            return ("Correction with no reason typed", $"{what} changed by hand with nothing written down.", "open", "Open line");
        if (l.Flags.HasFlag(LedgerFlags.OutsideHours))
            return ("Change made outside working hours", $"Posted at {l.Time:HH:mm} from {l.IpAddress ?? "an unknown address"}.", "open", "Open line");
        return ("Large value in one line", $"{what} is worth more than {Money(s.LargeValueOver)}.", "open", "Open line");
    }

    private async Task<int?> PairOfAsync(StockLedgerLine line, StockLedgerSetting s)
    {
        var since = line.Time.AddMinutes(-s.QuicklyReversedMinutes);
        var opposite = -line.Change;
        var q = (await _lines.GetQueryableAsync())
            .Where(x => x.Id != line.Id && x.ProductId == line.ProductId && x.WarehouseId == line.WarehouseId
                        && x.Time >= since && x.Time <= line.Time && x.Change == opposite)
            .OrderByDescending(x => x.Id).Select(x => (int?)x.Id).Take(1);
        return await AsyncExecuter.FirstOrDefaultAsync(q);
    }

    private async Task<List<LedgerHandCorrectorDto>> HandCorrectorsAsync()
    {
        var since = Clock.Now.Date.AddDays(-90);
        var hand = new[] { LedgerMovement.CountCorrection, LedgerMovement.Damaged, LedgerMovement.Lost };
        var rows = await AsyncExecuter.ToListAsync((await _lines.GetQueryableAsync())
            .Where(l => l.Time >= since && hand.Contains(l.Movement))
            .Select(l => new { l.UserId, l.UserName, l.UserRole, l.Change, l.UnitCost }));
        if (rows.Count == 0) return new();
        var total = rows.Count;
        return rows.GroupBy(r => new { r.UserId, r.UserName })
            .Select(g => new LedgerHandCorrectorDto
            {
                UserId = g.Key.UserId, Name = g.Key.UserName, Role = g.Select(x => x.UserRole).FirstOrDefault(x => x != null),
                Count = g.Count(), Value = Math.Round(g.Sum(x => Math.Abs(x.Change) * x.UnitCost), 2),
                Percent = (int)Math.Round(g.Count() * 100.0 / total),
            })
            .OrderByDescending(c => c.Count).ThenByDescending(c => c.Value).ToList();
    }

    [Authorize(DymoEnergyPermissions.Stock.Ledger)]
    public async Task<LedgerLineDetailDto> ReviewLineAsync(int id, ReviewLedgerLineDto input)
    {
        await _lines.GetAsync(id);
        var action = input.Action is "checked" or "asked" or "reversed" ? input.Action : "checked";
        await _reviews.InsertAsync(new StockLedgerReview
        {
            LineId = id, Time = Clock.Now, UserId = CurrentUser.Id,
            UserName = CurrentUser.UserName ?? CurrentUser.Name ?? "Someone", Action = action,
            Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
        }, autoSave: true);
        return await GetLineAsync(id);
    }

    // ══ WHO HAS ACCESS ═══════════════════════════════════════════════════════

    public async Task<LedgerAccessDto> GetAccessAsync()
    {
        var dto = new LedgerAccessDto { Setting = MapSetting(await _writer.SettingAsync()) };

        var users = (await _users.GetListAsync(maxResultCount: 50, includeDetails: true)).ToList();
        var roles = (await _roles.GetListAsync()).ToDictionary(r => r.Id, r => r.Name);
        var abilities = new Dictionary<string, string>();
        foreach (var roleName in roles.Values.Distinct())
            abilities[roleName] = await StockAbilitiesAsync(roleName);

        var postedBy = (await AsyncExecuter.ToListAsync((await _lines.GetQueryableAsync()).Select(l => l.UserId)))
            .Where(u => u != null).GroupBy(u => u!.Value).ToDictionary(g => g.Key, g => g.Count());

        var signIns = await SignInsAsync();
        foreach (var u in users)
        {
            var names = u.Roles.Select(r => roles.GetValueOrDefault(r.RoleId)).Where(n => n != null).Select(n => n!).ToList();
            var last = signIns.Where(s => s.UserId == u.Id && s.Ok).OrderByDescending(s => s.Time).FirstOrDefault();
            dto.People.Add(new LedgerPersonDto
            {
                Id = u.Id, Name = string.IsNullOrWhiteSpace(u.Name) ? u.UserName : $"{u.Name} {u.Surname}".Trim(),
                Email = u.Email, Role = names.Count > 0 ? string.Join(", ", names) : "No role",
                MayDo = names.Select(n => abilities.GetValueOrDefault(n, "")).FirstOrDefault(a => !string.IsNullOrEmpty(a)) ?? "Nothing in stock",
                LastSignedIn = last?.Time, LastIp = last?.Ip, LastDevice = last?.Device,
                TwoStep = u.TwoFactorEnabled, LinesPosted = postedBy.GetValueOrDefault(u.Id),
            });
        }
        dto.People = dto.People.OrderByDescending(p => p.LinesPosted).ThenBy(p => p.Name).ToList();

        dto.SecurityLogsAvailable = signIns.Count > 0;
        dto.Attempts = signIns.Where(s => !s.Ok || s.Time >= Clock.Now.AddDays(-7)).OrderByDescending(s => s.Time).Take(12)
            .Select(s => new LedgerSignInAttemptDto
            {
                Time = s.Time, Who = s.Who, What = s.Action, Ip = s.Ip, Device = s.Device,
                Tone = s.Ok ? "ok" : s.Blocked ? "blocked" : "warn",
                Result = s.Ok ? "Signed in" : s.Blocked ? "Blocked" : "Failed",
            }).ToList();

        var machines = await AsyncExecuter.ToListAsync((await _lines.GetQueryableAsync())
            .GroupBy(l => l.Source)
            .Select(g => new { Source = g.Key, Lines = g.Count(), LastUsed = g.Max(x => x.Time), Devices = g.Select(x => x.Device).Distinct().Count() }));
        dto.Machines = machines.Select(m => new LedgerMachineDto
        {
            Source = m.Source, Name = SourceLabel(m.Source), Lines = m.Lines, LastUsed = m.LastUsed,
            Detail = $"{m.Devices} {(m.Devices == 1 ? "device" : "devices")} · {m.Lines:#,##0} {(m.Lines == 1 ? "line" : "lines")}",
        }).OrderByDescending(m => m.Lines).ToList();
        return dto;
    }

    private async Task<string> StockAbilitiesAsync(string roleName)
    {
        try
        {
            var granted = (await _permissions.GetAllAsync(RoleProvider, roleName)).Where(p => p.IsGranted).Select(p => p.Name).ToHashSet();
            var can = new List<string>();
            if (granted.Contains(DymoEnergyPermissions.Stock.Edit)) can.Add("prepare entries");
            if (granted.Contains(DymoEnergyPermissions.Stock.Post)) can.Add("post and reverse");
            if (granted.Contains(DymoEnergyPermissions.Stock.LedgerSettings)) can.Add("change ledger rules");
            else if (granted.Contains(DymoEnergyPermissions.Stock.Ledger)) can.Add("read the ledger");
            return can.Count == 0 ? "Nothing in stock" : char.ToUpperInvariant(can[0][0]) + string.Join(", ", can)[1..];
        }
        catch (Exception)
        {
            // A role that was removed between the two reads should not break the page.
            return "Unknown";
        }
    }

    /// <summary>One sign-in attempt read from the identity security log.</summary>
    private sealed class SignIn
    {
        public Guid? UserId { get; init; }
        public string Who { get; init; } = string.Empty;
        public DateTime Time { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? Ip { get; init; }
        public string? Device { get; init; }
        public bool Ok { get; init; }
        public bool Blocked { get; init; }
    }

    private async Task<List<SignIn>> SignInsAsync()
    {
        var since = Clock.Now.AddDays(-30);
        var rows = await AsyncExecuter.ToListAsync((await _securityLogs.GetQueryableAsync())
            .Where(s => s.CreationTime >= since && s.Action != null && s.Action.Contains("Login"))
            .OrderByDescending(s => s.CreationTime).Take(200)
            .Select(s => new { s.UserId, s.UserName, s.CreationTime, s.Action, s.ClientIpAddress, s.BrowserInfo }));

        return rows.Select(r =>
        {
            var action = r.Action ?? "";
            var ok = action.Contains("Succeeded", StringComparison.OrdinalIgnoreCase);
            var blocked = action.Contains("Locked", StringComparison.OrdinalIgnoreCase) || action.Contains("NotAllowed", StringComparison.OrdinalIgnoreCase);
            return new SignIn
            {
                UserId = r.UserId, Who = r.UserName ?? "Unknown account", Time = r.CreationTime, Action = Spaced(action),
                Ip = r.ClientIpAddress, Device = r.BrowserInfo, Ok = ok, Blocked = blocked,
            };
        }).ToList();
    }

    /// <summary>"LoginInvalidPassword" → "Login invalid password".</summary>
    private static string Spaced(string s) =>
        string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : c.ToString()));

    // ══ PROOF & KEEPING ══════════════════════════════════════════════════════

    public async Task<LedgerProofDto> GetProofAsync()
    {
        var setting = await _writer.SettingAsync();
        var q = await _lines.GetQueryableAsync();
        var newest = await AsyncExecuter.ToListAsync(q.OrderByDescending(l => l.Id).Take(3));
        var first = await AsyncExecuter.FirstOrDefaultAsync(q.OrderBy(l => l.Id).Take(1));
        var last = await AsyncExecuter.FirstOrDefaultAsync((await _checks.GetQueryableAsync()).OrderByDescending(c => c.Id).Take(1));

        var seals = newest.Select(l => MapSeal(l, false)).ToList();
        if (first != null && newest.All(n => n.Id != first.Id)) seals.Add(MapSeal(first, true));
        else if (first != null && seals.Count > 0) seals[^1] = MapSeal(first, true);

        return new LedgerProofDto
        {
            Seals = seals, TotalLines = await AsyncExecuter.LongCountAsync(q), Setting = MapSetting(setting),
            LastExportAt = setting.LastExportAt, LastExportBy = setting.LastExportBy,
            LastCheck = last == null ? null : new LedgerCheckResultDto
            {
                Ok = last.Ok, LinesChecked = last.LinesChecked, Time = last.Time, DurationMs = last.DurationMs,
                FirstBadLineId = last.FirstBadLineId,
                Message = last.Ok ? $"All {last.LinesChecked:#,##0} seals match." : $"Line {last.FirstBadLineId:#,##0} does not match its seal.",
            },
        };
    }

    private static LedgerSealDto MapSeal(StockLedgerLine l, bool isFirst) => new()
    {
        LineId = l.Id, Time = l.Time, Hash = l.Hash, PreviousHash = l.PreviousHash, IsFirst = isFirst,
        Title = isFirst ? "opening line" : $"{MovementLabel(l.Movement).ToLowerInvariant()} · {l.ProductName}",
    };

    /// <summary>
    /// Reads every line in order, recomputes its seal and checks it still points at the line before it.
    /// A changed value, a removed line or a line slipped in between all show up here.
    /// </summary>
    public async Task<LedgerCheckResultDto> VerifyChainAsync()
    {
        var watch = Stopwatch.StartNew();
        var q = await _lines.GetQueryableAsync();
        var previous = LedgerConsts.GenesisHash;
        int checkedLines = 0;
        int? firstBad = null;
        const int batch = 500;

        for (var skip = 0; firstBad == null; skip += batch)
        {
            var page = await AsyncExecuter.ToListAsync(q.OrderBy(l => l.Id).Skip(skip).Take(batch));
            if (page.Count == 0) break;
            foreach (var line in page)
            {
                checkedLines++;
                if (line.PreviousHash != previous || LedgerWriter.Seal(line) != line.Hash) { firstBad = line.Id; break; }
                previous = line.Hash;
            }
            if (page.Count < batch) break;
        }

        watch.Stop();
        var result = new LedgerCheckResultDto
        {
            Ok = firstBad == null, LinesChecked = checkedLines, Time = Clock.Now, DurationMs = (int)watch.ElapsedMilliseconds,
            FirstBadLineId = firstBad,
            Message = firstBad == null
                ? $"All {checkedLines:#,##0} seals match."
                : $"Line {firstBad:#,##0} does not match its seal. Everything before it is still sound.",
        };
        await _checks.InsertAsync(new StockLedgerCheck
        {
            Time = result.Time, LinesChecked = result.LinesChecked, Ok = result.Ok, FirstBadLineId = firstBad,
            DurationMs = result.DurationMs, StartedBy = "person", UserName = CurrentUser.UserName,
        }, autoSave: true);
        return result;
    }

    // ══ SETTINGS ═════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Stock.LedgerSettings)]
    public async Task<LedgerSettingDto> UpdateSettingAsync(LedgerSettingDto input)
    {
        var s = await _writer.SettingAsync();
        s.FlagNoReason = input.FlagNoReason; s.FlagNoPhoto = input.FlagNoPhoto;
        s.FlagQuicklyReversed = input.FlagQuicklyReversed; s.QuicklyReversedMinutes = Math.Clamp(input.QuicklyReversedMinutes, 1, 240);
        s.FlagOutsideHours = input.FlagOutsideHours;
        s.WorkingFromHour = Math.Clamp(input.WorkingFromHour, 0, 23);
        s.WorkingToHour = Math.Clamp(input.WorkingToHour, s.WorkingFromHour + 1, 24);
        s.FlagBelowZero = input.FlagBelowZero; s.FlagLargeValue = input.FlagLargeValue;
        s.LargeValueOver = Math.Round(input.LargeValueOver, 2);
        s.AskPasswordAgain = input.AskPasswordAgain; s.TwoPeopleForBigWriteOffs = input.TwoPeopleForBigWriteOffs;
        s.RequireTwoStep = input.RequireTwoStep; s.OnlyFromOfficeNetworks = input.OnlyFromOfficeNetworks;
        s.AllowedNetworks = string.IsNullOrWhiteSpace(input.AllowedNetworks) ? null : input.AllowedNetworks.Trim();
        s.EmailOwnerOnReversal = input.EmailOwnerOnReversal;
        s.KeepYears = Math.Clamp(input.KeepYears, 1, 50);
        await _settings.UpdateAsync(s, autoSave: true);
        return MapSetting(s);
    }

    private static LedgerSettingDto MapSetting(StockLedgerSetting s) => new()
    {
        FlagNoReason = s.FlagNoReason, FlagNoPhoto = s.FlagNoPhoto, FlagQuicklyReversed = s.FlagQuicklyReversed,
        QuicklyReversedMinutes = s.QuicklyReversedMinutes, FlagOutsideHours = s.FlagOutsideHours,
        WorkingFromHour = s.WorkingFromHour, WorkingToHour = s.WorkingToHour, FlagBelowZero = s.FlagBelowZero,
        FlagLargeValue = s.FlagLargeValue, LargeValueOver = s.LargeValueOver, AskPasswordAgain = s.AskPasswordAgain,
        TwoPeopleForBigWriteOffs = s.TwoPeopleForBigWriteOffs, RequireTwoStep = s.RequireTwoStep,
        OnlyFromOfficeNetworks = s.OnlyFromOfficeNetworks, AllowedNetworks = s.AllowedNetworks,
        EmailOwnerOnReversal = s.EmailOwnerOnReversal, KeepYears = s.KeepYears,
    };
}
