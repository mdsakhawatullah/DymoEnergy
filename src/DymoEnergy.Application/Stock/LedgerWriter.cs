using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace DymoEnergy.Stock;

/// <summary>What the ledger is told about one movement, before it is sealed and written.</summary>
public class LedgerMove
{
    public int      ProductId      { get; set; }
    public string   ProductName    { get; set; } = string.Empty;
    public string?  Sku            { get; set; }
    public int      WarehouseId    { get; set; }
    public string   WarehouseName  { get; set; } = string.Empty;
    public LedgerMovement Movement { get; set; }
    public int      Change         { get; set; }
    public int      QuantityBefore { get; set; }
    public int      QuantityAfter  { get; set; }
    public decimal  UnitCost       { get; set; }
    public decimal  CostBefore     { get; set; }
    public decimal  CostAfter      { get; set; }
    public List<string> Serials    { get; set; } = new();
    public string?  Reason         { get; set; }
    public string?  DocumentType   { get; set; }
    public string?  DocumentNumber { get; set; }
    public int?     StockEntryId   { get; set; }
    public int?     ReversesLineId { get; set; }
    public string?  CameFrom       { get; set; }
    /// <summary>Set when the move is a hand correction or a write-off, which the flag rules watch.</summary>
    public bool     IsHandCorrection { get; set; }
    public bool     HasPhoto       { get; set; }
    public Guid?    ApprovedById   { get; set; }
    public string?  ApprovedByName { get; set; }
    public LedgerFlags ExtraFlags  { get; set; }
}

/// <summary>
/// Appends sealed lines to the stock ledger. Nothing else may write to the ledger table, and nothing —
/// including this class — ever changes or removes a line that is already there.
/// </summary>
public class LedgerWriter : ITransientDependency
{
    /// <summary>Appends run one at a time so two posts cannot claim the same place in the chain.</summary>
    private static readonly SemaphoreSlim ChainLock = new(1, 1);

    private readonly IRepository<StockLedgerLine, int> _lines;
    private readonly IRepository<StockLedgerSetting, int> _settings;
    private readonly IAsyncQueryableExecuter _async;
    private readonly ICurrentUser _currentUser;
    private readonly ILedgerRequestInfo _client;
    private readonly IClock _clock;

    public LedgerWriter(IRepository<StockLedgerLine, int> lines, IRepository<StockLedgerSetting, int> settings,
        IAsyncQueryableExecuter async, ICurrentUser currentUser, ILedgerRequestInfo client, IClock clock)
    {
        _lines = lines; _settings = settings; _async = async; _currentUser = currentUser; _client = client; _clock = clock;
    }

    // ── Settings ────────────────────────────────────────────────────────────

    /// <summary>The single settings row, created with sensible defaults the first time it is needed.</summary>
    public async Task<StockLedgerSetting> SettingAsync()
    {
        var existing = (await _settings.GetListAsync()).OrderBy(s => s.Id).FirstOrDefault();
        if (existing != null) return existing;
        await ChainLock.WaitAsync();
        try
        {
            existing = (await _settings.GetListAsync()).OrderBy(s => s.Id).FirstOrDefault();
            return existing ?? await _settings.InsertAsync(new StockLedgerSetting(), autoSave: true);
        }
        finally
        {
            ChainLock.Release();
        }
    }

    // ── Writing ─────────────────────────────────────────────────────────────

    /// <summary>Seals and appends one line per movement, in the order given.</summary>
    public async Task<List<StockLedgerLine>> AppendAsync(IEnumerable<LedgerMove> moves)
    {
        var list = moves.ToList();
        var written = new List<StockLedgerLine>();
        if (list.Count == 0) return written;

        var setting = await SettingAsync();
        // Trimmed to whole milliseconds: the seal must survive the trip through the database unchanged.
        var now = Trim(_clock.Now);
        var zone = TimeZoneText();
        var user = UserFacts();

        await ChainLock.WaitAsync();
        try
        {
            var previous = await LastHashAsync();
            foreach (var m in list)
            {
                var line = new StockLedgerLine
                {
                    ProductId = m.ProductId, ProductName = Cut(m.ProductName, LedgerConsts.MaxName)!, Sku = Cut(m.Sku, LedgerConsts.MaxShort),
                    WarehouseId = m.WarehouseId, WarehouseName = Cut(m.WarehouseName, LedgerConsts.MaxName)!,
                    Movement = m.Movement, Change = m.Change, QuantityBefore = m.QuantityBefore, QuantityAfter = m.QuantityAfter,
                    UnitCost = Round(m.UnitCost), ValueBefore = Round(m.QuantityBefore * m.CostBefore), ValueAfter = Round(m.QuantityAfter * m.CostAfter),
                    Serials = m.Serials.Count == 0 ? null : string.Join('\n', m.Serials),
                    Time = now, TimeZone = zone,
                    UserId = user.Id, UserName = user.Name, UserEmail = user.Email, UserRole = user.Role,
                    SignedInAt = user.SignedInAt, TwoStepUsed = user.TwoStep,
                    ApprovedById = m.ApprovedById, ApprovedByName = Cut(m.ApprovedByName, LedgerConsts.MaxName),
                    Source = user.Source, IpAddress = Cut(_client.IpAddress, 64), Device = Cut(_client.Device, LedgerConsts.MaxName),
                    SessionId = user.SessionId, CameFrom = Cut(m.CameFrom, LedgerConsts.MaxShort),
                    Reason = Cut(m.Reason, LedgerConsts.MaxReason), DocumentType = Cut(m.DocumentType, 64),
                    DocumentNumber = Cut(m.DocumentNumber, LedgerConsts.MaxShort), StockEntryId = m.StockEntryId,
                    ReversesLineId = m.ReversesLineId, ServerName = Cut(Environment.MachineName, 64),
                    PreviousHash = previous,
                };
                line.Flags = FlagsFor(line, m, setting) | await QuickReversalFlagAsync(line, setting);
                line.Hash = Seal(line);
                await _lines.InsertAsync(line, autoSave: true);
                previous = line.Hash;
                written.Add(line);
            }
        }
        finally
        {
            ChainLock.Release();
        }

        return written;
    }

    private async Task<string> LastHashAsync()
    {
        var q = (await _lines.GetQueryableAsync()).OrderByDescending(l => l.Id).Select(l => l.Hash).Take(1);
        return (await _async.FirstOrDefaultAsync(q)) ?? LedgerConsts.GenesisHash;
    }

    // ── The seal ────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything that matters about the line, joined in a fixed order, then hashed together with the seal
    /// of the line before it. Changing any one of these values makes this seal — and every seal after it — wrong.
    /// </summary>
    public static string Content(StockLedgerLine l) => string.Join('\u001f', new[]
    {
        l.Time.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
        l.ProductId.ToString(CultureInfo.InvariantCulture),
        l.WarehouseId.ToString(CultureInfo.InvariantCulture),
        ((int)l.Movement).ToString(CultureInfo.InvariantCulture),
        l.Change.ToString(CultureInfo.InvariantCulture),
        l.QuantityBefore.ToString(CultureInfo.InvariantCulture),
        l.QuantityAfter.ToString(CultureInfo.InvariantCulture),
        l.UnitCost.ToString("0.00", CultureInfo.InvariantCulture),
        l.ValueBefore.ToString("0.00", CultureInfo.InvariantCulture),
        l.ValueAfter.ToString("0.00", CultureInfo.InvariantCulture),
        l.Serials ?? "",
        l.UserId?.ToString() ?? "",
        l.UserName,
        ((int)l.Source).ToString(CultureInfo.InvariantCulture),
        l.IpAddress ?? "",
        l.SessionId ?? "",
        l.Reason ?? "",
        l.DocumentType ?? "",
        l.DocumentNumber ?? "",
        l.StockEntryId?.ToString(CultureInfo.InvariantCulture) ?? "",
        l.ReversesLineId?.ToString(CultureInfo.InvariantCulture) ?? "",
        ((int)l.Flags).ToString(CultureInfo.InvariantCulture),
    });

    public static string Seal(StockLedgerLine line) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Content(line) + '\u001e' + line.PreviousHash))).ToLowerInvariant();

    // ── Flags ───────────────────────────────────────────────────────────────

    private static LedgerFlags FlagsFor(StockLedgerLine line, LedgerMove move, StockLedgerSetting s)
    {
        var flags = move.ExtraFlags;
        if (move.IsHandCorrection)
        {
            if (s.FlagNoReason && string.IsNullOrWhiteSpace(line.Reason)) flags |= LedgerFlags.NoReason;
            if (s.FlagNoPhoto && !move.HasPhoto) flags |= LedgerFlags.NoPhoto;
        }
        if (s.FlagOutsideHours && (line.Time.Hour < s.WorkingFromHour || line.Time.Hour >= s.WorkingToHour)) flags |= LedgerFlags.OutsideHours;
        if (s.FlagLargeValue && Math.Abs(line.Change) * line.UnitCost > s.LargeValueOver)
        {
            flags |= LedgerFlags.LargeValue;
            if (s.TwoPeopleForBigWriteOffs && line.Change < 0 && line.ApprovedById == null) flags |= LedgerFlags.NotApproved;
        }
        return flags;
    }

    /// <summary>
    /// True when this line undoes an opposite move on the same product and warehouse made minutes ago, which
    /// often means someone was trying numbers until they fit. Worked out before the line is sealed, because
    /// the seal covers the flags and a sealed line is never touched again.
    /// </summary>
    private async Task<LedgerFlags> QuickReversalFlagAsync(StockLedgerLine line, StockLedgerSetting s)
    {
        if (!s.FlagQuicklyReversed || line.Change == 0) return LedgerFlags.None;
        var since = line.Time.AddMinutes(-s.QuicklyReversedMinutes);
        var opposite = -line.Change;
        var q = (await _lines.GetQueryableAsync())
            .Where(l => l.ProductId == line.ProductId && l.WarehouseId == line.WarehouseId && l.Time >= since && l.Change == opposite);
        return await _async.AnyAsync(q) ? LedgerFlags.QuicklyReversed : LedgerFlags.None;
    }

    // ── Who and what ────────────────────────────────────────────────────────

    private (Guid? Id, string Name, string? Email, string? Role, Guid? Tenant, DateTime? SignedInAt, bool TwoStep, string? SessionId, LedgerSource Source) UserFacts()
    {
        var u = _currentUser;
        var name = string.Join(" ", new[] { u.Name, u.SurName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(name)) name = u.UserName ?? (u.IsAuthenticated ? "Signed-in user" : "System");
        var amr = u.FindClaimValue("amr");
        var session = u.FindClaimValue("sid") ?? u.FindClaimValue("session_id");
        DateTime? signedIn = long.TryParse(u.FindClaimValue("auth_time"), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).ToLocalTime().DateTime : null;
        var source = !u.IsAuthenticated ? LedgerSource.System
            : u.FindClaimValue("client_id") is { Length: > 0 } client && client.Contains("pos", StringComparison.OrdinalIgnoreCase) ? LedgerSource.Pos
            : LedgerSource.Screen;
        return (u.Id, Cut(name, LedgerConsts.MaxName)!, Cut(u.Email, LedgerConsts.MaxName), Cut(u.Roles.FirstOrDefault(), LedgerConsts.MaxShort),
                u.TenantId, signedIn, amr != null && (amr.Contains("mfa") || amr.Contains("otp")), Cut(session, 64), source);
    }

    private string TimeZoneText()
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(_clock.Now);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var hours = Math.Abs(offset.Hours) + (Math.Abs(offset.Minutes) > 0 ? Math.Abs(offset.Minutes) / 60m : 0);
        return $"UTC{sign}{hours.ToString("0.##", CultureInfo.InvariantCulture)}";
    }

    /// <summary>Whole milliseconds, and no time-zone kind, so the stored time and the sealed time always agree.</summary>
    private static DateTime Trim(DateTime t) =>
        new(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second, t.Millisecond, DateTimeKind.Unspecified);

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static string? Cut(string? s, int max) => s == null ? null : s.Length > max ? s[..max] : s;
}
