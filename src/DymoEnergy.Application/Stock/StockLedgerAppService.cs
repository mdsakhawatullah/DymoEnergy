using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace DymoEnergy.Stock;

[Authorize(DymoEnergyPermissions.Stock.Ledger)]
public partial class StockLedgerAppService : ApplicationService, IStockLedgerAppService
{
    private readonly IRepository<StockLedgerLine, int>    _lines;
    private readonly IRepository<StockLedgerReview, int>  _reviews;
    private readonly IRepository<StockLedgerCheck, int>   _checks;
    private readonly IRepository<StockLedgerSetting, int> _settings;
    private readonly IRepository<StockBalance, int>       _balances;
    private readonly IRepository<Product, int>            _products;
    private readonly IRepository<StockEntryAttachment, int> _attachments;
    private readonly IRepository<StockSerial, int>        _serials;
    private readonly IIdentityUserRepository              _users;
    private readonly IRepository<IdentityRole, Guid>      _roles;
    private readonly IRepository<IdentitySecurityLog, Guid> _securityLogs;
    private readonly IPermissionManager                   _permissions;
    private readonly LedgerWriter _writer;
    private readonly StockLedger  _stock;

    public StockLedgerAppService(
        IRepository<StockLedgerLine, int> lines, IRepository<StockLedgerReview, int> reviews, IRepository<StockLedgerCheck, int> checks,
        IRepository<StockLedgerSetting, int> settings, IRepository<StockBalance, int> balances, IRepository<Product, int> products,
        IRepository<StockEntryAttachment, int> attachments, IRepository<StockSerial, int> serials,
        IIdentityUserRepository users, IRepository<IdentityRole, Guid> roles, IRepository<IdentitySecurityLog, Guid> securityLogs,
        IPermissionManager permissions, LedgerWriter writer, StockLedger stock)
    {
        _lines = lines; _reviews = reviews; _checks = checks; _settings = settings; _balances = balances; _products = products;
        _attachments = attachments; _serials = serials; _users = users; _roles = roles; _securityLogs = securityLogs;
        _permissions = permissions; _writer = writer; _stock = stock;
    }

    // ══ LABELS ═══════════════════════════════════════════════════════════════

    public static string MovementLabel(LedgerMovement m) => m switch
    {
        LedgerMovement.Opening => "Opening count",
        LedgerMovement.Received => "Received",
        LedgerMovement.Sold => "Sold",
        LedgerMovement.UsedOnJob => "Used on job",
        LedgerMovement.TransferredOut => "Transferred out",
        LedgerMovement.TransferredIn => "Transferred in",
        LedgerMovement.CountCorrection => "Count correction",
        LedgerMovement.Damaged => "Damaged",
        LedgerMovement.ReturnedToSupplier => "Returned to supplier",
        LedgerMovement.Lost => "Lost or stolen",
        LedgerMovement.InternalUse => "Used internally",
        LedgerMovement.Reversal => "Reversal",
        _ => "Other",
    };

    public static string SourceLabel(LedgerSource s) => s switch
    {
        LedgerSource.Screen => "Admin screen",
        LedgerSource.Pos => "POS tablet",
        LedgerSource.Storefront => "Storefront order",
        LedgerSource.Api => "API key",
        LedgerSource.Import => "Import",
        _ => "The app itself",
    };

    private static List<string> FlagLabels(LedgerFlags f)
    {
        var list = new List<string>();
        if (f.HasFlag(LedgerFlags.NoReason)) list.Add("No reason typed");
        if (f.HasFlag(LedgerFlags.NoPhoto)) list.Add("No photo attached");
        if (f.HasFlag(LedgerFlags.QuicklyReversed)) list.Add("Reversed within minutes");
        if (f.HasFlag(LedgerFlags.OutsideHours)) list.Add("Outside working hours");
        if (f.HasFlag(LedgerFlags.WouldGoBelowZero)) list.Add("Stock would have gone below zero");
        if (f.HasFlag(LedgerFlags.LargeValue)) list.Add("Large value");
        if (f.HasFlag(LedgerFlags.SerialNotReceived)) list.Add("Serial never received");
        if (f.HasFlag(LedgerFlags.NotApproved)) list.Add("No second person approved it");
        return list;
    }

    private static bool IsHandCorrection(LedgerMovement m) =>
        m is LedgerMovement.CountCorrection or LedgerMovement.Damaged or LedgerMovement.Lost;

    // ══ HEADER ═══════════════════════════════════════════════════════════════

    public async Task<LedgerHeaderDto> GetHeaderAsync()
    {
        await EnsureOpeningLinesAsync();
        var today = Clock.Now.Date;
        var q = await _lines.GetQueryableAsync();
        var todayLines = await AsyncExecuter.ToListAsync(q.Where(l => l.Time >= today)
            .Select(l => new { l.UserId, l.Source, l.Change, l.ProductId, l.Movement, l.Flags }));

        var reviewed = (await AsyncExecuter.ToListAsync((await _reviews.GetQueryableAsync()).Select(r => r.LineId))).ToHashSet();
        var flagged = await AsyncExecuter.ToListAsync(q.Where(l => l.Flags != LedgerFlags.None).Select(l => new { l.Id, l.Time }));
        var open = flagged.Where(f => !reviewed.Contains(f.Id)).ToList();

        var last = await AsyncExecuter.FirstOrDefaultAsync((await _checks.GetQueryableAsync()).OrderByDescending(c => c.Id).Take(1));
        var inLines = todayLines.Where(l => l.Change > 0 && l.Movement != LedgerMovement.TransferredIn).ToList();

        return new LedgerHeaderDto
        {
            LinesToday = todayLines.Count,
            PeopleToday = todayLines.Where(l => l.UserId != null).Select(l => l.UserId).Distinct().Count(),
            MachinesToday = todayLines.Where(l => l.Source != LedgerSource.Screen).Select(l => l.Source).Distinct().Count(),
            InUnits = inLines.Sum(l => l.Change),
            InProducts = inLines.Select(l => l.ProductId).Distinct().Count(),
            OutUnits = todayLines.Where(l => l.Change < 0 && l.Movement != LedgerMovement.TransferredOut).Sum(l => l.Change),
            HandCorrections = todayLines.Count(l => IsHandCorrection(l.Movement)),
            FlaggedOpen = open.Count,
            OldestFlaggedDays = open.Count == 0 ? null : (int)(Clock.Now.Date - open.Min(f => f.Time).Date).TotalDays,
            TotalLines = await AsyncExecuter.LongCountAsync(q),
            LastCheckAt = last?.Time, LastCheckOk = last?.Ok ?? true, LastCheckLines = last?.LinesChecked ?? 0,
        };
    }

    /// <summary>
    /// Stock that was already on the shelves before the ledger existed is written in once, as an opening count
    /// per product and warehouse. Without it the first real movement would start from a quantity with nothing
    /// behind it. The lines say plainly that the app, not a person, wrote them.
    /// </summary>
    private async Task EnsureOpeningLinesAsync()
    {
        if (await _lines.AnyAsync()) return;

        // Products that have stock but were never through a stock entry get a balance row first.
        var standing = await AsyncExecuter.ToListAsync((await _products.GetQueryableAsync())
            .Where(p => p.StockQuantity > 0).Select(p => p.Id));
        if (standing.Count > 0) await _stock.BalancesAsync(standing);

        var balances = (await _balances.GetListAsync()).Where(b => b.Quantity != 0).ToList();
        if (balances.Count == 0) return;

        var productIds = balances.Select(b => b.ProductId).Distinct().ToList();
        var products = (await AsyncExecuter.ToListAsync((await _products.GetQueryableAsync())
            .Where(p => productIds.Contains(p.Id)).Select(p => new { p.Id, p.Name, p.Sku })))
            .ToDictionary(p => p.Id, p => (Name: p.Name ?? $"Product #{p.Id}", p.Sku));
        var warehouses = (await _stock.WarehousesAsync()).ToDictionary(w => w.Id, w => w.Name);

        var moves = balances
            .OrderBy(b => products.GetValueOrDefault(b.ProductId).Name).ThenBy(b => b.WarehouseId)
            .Select(b => new LedgerMove
            {
                ProductId = b.ProductId,
                ProductName = products.GetValueOrDefault(b.ProductId).Name ?? $"Product #{b.ProductId}",
                Sku = products.GetValueOrDefault(b.ProductId).Sku,
                WarehouseId = b.WarehouseId, WarehouseName = warehouses.GetValueOrDefault(b.WarehouseId, "Warehouse"),
                Movement = LedgerMovement.Opening, Change = b.Quantity, QuantityBefore = 0, QuantityAfter = b.Quantity,
                UnitCost = b.AvgCost, CostBefore = 0, CostAfter = b.AvgCost,
                Reason = "What was on the shelf when the ledger started. Counted before this page existed, so no earlier line explains it.",
                DocumentType = "Opening count", CameFrom = "Ledger opening",
            }).ToList();

        await _writer.AppendAsync(moves);
    }

    // ══ ALL MOVEMENTS ════════════════════════════════════════════════════════

    public async Task<LedgerLinesPageDto> GetLinesAsync(GetLedgerLinesInput input)
    {
        var q = await FilteredAsync(input);
        var total = await AsyncExecuter.LongCountAsync(q);
        var page = await AsyncExecuter.ToListAsync(q.OrderByDescending(l => l.Id).Skip(input.SkipCount).Take(input.MaxResultCount));
        return new LedgerLinesPageDto { TotalCount = total, Items = await MapLinesAsync(page) };
    }

    private async Task<IQueryable<StockLedgerLine>> FilteredAsync(GetLedgerLinesInput input)
    {
        var q = await _lines.GetQueryableAsync();
        if (input.ProductId is { } pid) q = q.Where(l => l.ProductId == pid);
        if (input.WarehouseId is { } wid) q = q.Where(l => l.WarehouseId == wid);
        if (input.UserId is { } uid) q = q.Where(l => l.UserId == uid);
        if (input.Movement is { } mv) q = q.Where(l => l.Movement == mv);
        if (input.FlaggedOnly) q = q.Where(l => l.Flags != LedgerFlags.None);
        if (input.Days is { } days && days > 0) { var from = Clock.Now.Date.AddDays(-days); q = q.Where(l => l.Time >= from); }
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLower();
            q = q.Where(l => l.ProductName.ToLower().Contains(f)
                || (l.Sku != null && l.Sku.ToLower().Contains(f))
                || (l.Serials != null && l.Serials.ToLower().Contains(f))
                || (l.DocumentNumber != null && l.DocumentNumber.ToLower().Contains(f))
                || l.UserName.ToLower().Contains(f)
                || (l.IpAddress != null && l.IpAddress.Contains(f)));
        }
        return q;
    }

    private async Task<List<LedgerLineDto>> MapLinesAsync(List<StockLedgerLine> lines)
    {
        if (lines.Count == 0) return new();
        var ids = lines.Select(l => l.Id).ToList();
        var reviewed = (await AsyncExecuter.ToListAsync((await _reviews.GetQueryableAsync()).Where(r => ids.Contains(r.LineId)).Select(r => r.LineId))).ToHashSet();
        return lines.Select(l => MapLine(l, reviewed.Contains(l.Id))).ToList();
    }

    private static LedgerLineDto MapLine(StockLedgerLine l, bool reviewed) => new()
    {
        Id = l.Id, Time = l.Time, ProductId = l.ProductId, ProductName = l.ProductName, Sku = l.Sku, WarehouseName = l.WarehouseName,
        Movement = l.Movement, MovementLabel = MovementLabel(l.Movement), Change = l.Change, QuantityBefore = l.QuantityBefore,
        QuantityAfter = l.QuantityAfter, UserName = l.UserName, IpAddress = l.IpAddress, DocumentNumber = l.DocumentNumber,
        Flags = l.Flags, FlagLabels = FlagLabels(l.Flags), Reviewed = reviewed,
    };

    // ══ ONE LINE ═════════════════════════════════════════════════════════════

    public async Task<LedgerLineDetailDto> GetLineAsync(int id)
    {
        var l = await _lines.GetAsync(id);
        var q = await _lines.GetQueryableAsync();
        var reviews = (await _reviews.GetListAsync(r => r.LineId == id)).OrderBy(r => r.Id).ToList();

        var dto = new LedgerLineDetailDto
        {
            Line = MapLine(l, reviews.Count > 0),
            Time = l.Time, TimeZone = l.TimeZone, WarehouseName = l.WarehouseName, UnitCost = l.UnitCost,
            ValueBefore = l.ValueBefore, ValueAfter = l.ValueAfter, Serials = LedgerSerials(l), Reason = l.Reason,
            UserId = l.UserId, UserEmail = l.UserEmail, UserRole = l.UserRole, SignedInAt = l.SignedInAt,
            SignedInText = SignedInText(l), TwoStepUsed = l.TwoStepUsed, ApprovedByName = l.ApprovedByName,
            NeededApproval = l.Flags.HasFlag(LedgerFlags.LargeValue),
            IpAddress = l.IpAddress, Device = l.Device, SessionId = l.SessionId, CameFrom = l.CameFrom,
            Source = l.Source, SourceLabel = SourceLabel(l.Source),
            Hash = l.Hash, PreviousHash = l.PreviousHash, SealOk = LedgerWriter.Seal(l) == l.Hash,
            ServerName = l.ServerName, RequestId = l.RequestId,
            PreviousLineId = await AsyncExecuter.FirstOrDefaultAsync(q.Where(x => x.Id < id).OrderByDescending(x => x.Id).Select(x => (int?)x.Id).Take(1)),
            NextLineId = await AsyncExecuter.FirstOrDefaultAsync(q.Where(x => x.Id > id).OrderBy(x => x.Id).Select(x => (int?)x.Id).Take(1)),
            Reviews = reviews.Select(r => new LedgerReviewDto { Time = r.Time, UserName = r.UserName, Action = r.Action, Note = r.Note }).ToList(),
        };

        dto.Changes = BuildChanges(l);
        dto.Paper = await BuildPaperAsync(l);
        return dto;
    }

    private static List<string> LedgerSerials(StockLedgerLine l) =>
        string.IsNullOrWhiteSpace(l.Serials) ? new() : l.Serials.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? SignedInText(StockLedgerLine l)
    {
        if (l.SignedInAt is not { } at) return null;
        var gap = l.Time - at;
        if (gap < TimeSpan.Zero) return null;
        var hours = (int)gap.TotalHours;
        var minutes = gap.Minutes;
        var parts = new List<string>();
        if (hours > 0) parts.Add($"{hours} {(hours == 1 ? "hour" : "hours")}");
        parts.Add($"{minutes} {(minutes == 1 ? "minute" : "minutes")}");
        return string.Join(" ", parts) + " before this line";
    }

    private static List<LedgerFieldChangeDto> BuildChanges(StockLedgerLine l)
    {
        var list = new List<LedgerFieldChangeDto>
        {
            new() { Field = "Quantity on hand", Was = l.QuantityBefore.ToString(), Became = l.QuantityAfter.ToString(), Changed = l.QuantityBefore != l.QuantityAfter },
            new() { Field = "Stock value", Was = Money(l.ValueBefore), Became = Money(l.ValueAfter), Changed = l.ValueBefore != l.ValueAfter },
            new() { Field = "Cost per unit", Was = Money(l.UnitCost), Became = Money(l.UnitCost), Changed = false },
        };
        if (l.Serials != null)
            list.Add(new LedgerFieldChangeDto { Field = "Serial numbers touched", Was = "—", Became = string.Join(", ", LedgerSerials(l)), Changed = true });
        return list;
    }

    private static string Money(decimal v) => "৳" + v.ToString("#,##0.##", CultureInfo.InvariantCulture);

    private async Task<List<LedgerPaperDto>> BuildPaperAsync(StockLedgerLine l)
    {
        var paper = new List<LedgerPaperDto>();
        if (l.DocumentNumber != null)
            paper.Add(new LedgerPaperDto
            {
                Icon = "file", Title = $"{l.DocumentType} {l.DocumentNumber}",
                Detail = l.Reason is { Length: > 0 } ? $"reason: {l.Reason}" : "no reason typed",
                Link = l.StockEntryId != null ? "stock-entry" : "none", LinkId = l.StockEntryId,
            });

        if (l.StockEntryId is { } entryId)
        {
            var files = await _attachments.GetListAsync(a => a.StockEntryId == entryId);
            paper.Add(files.Count > 0
                ? new LedgerPaperDto { Icon = "image", Title = $"{files.Count} {(files.Count == 1 ? "attachment" : "attachments")}", Detail = string.Join(", ", files.Take(3).Select(f => f.FileName)), Link = "stock-entry", LinkId = entryId }
                : new LedgerPaperDto { Icon = "image", Title = "No photo attached", Detail = "the correction rules ask for one", Link = "stock-entry", LinkId = entryId });
        }

        if (l.ReversesLineId is { } reversed)
            paper.Add(new LedgerPaperDto { Icon = "undo", Title = $"Puts line {reversed:#,##0} right", Link = "ledger-line", LinkId = reversed });

        var earlier = await AsyncExecuter.FirstOrDefaultAsync((await _lines.GetQueryableAsync())
            .Where(x => x.ProductId == l.ProductId && x.Id < l.Id).OrderByDescending(x => x.Id).Take(1));
        if (earlier != null)
            paper.Add(new LedgerPaperDto
            {
                Icon = "list", Title = "Previous line for this product",
                Detail = $"{MovementLabel(earlier.Movement)} on {earlier.Time:d MMM}", Link = "ledger-line", LinkId = earlier.Id,
            });
        return paper;
    }

    // ══ ONE PRODUCT ══════════════════════════════════════════════════════════

    public async Task<List<LedgerProductOptionDto>> GetProductOptionsAsync()
    {
        var ids = await AsyncExecuter.ToListAsync((await _lines.GetQueryableAsync()).Select(l => l.ProductId).Distinct());
        var q = (await _products.GetQueryableAsync()).Where(p => ids.Contains(p.Id)).OrderBy(p => p.Name)
            .Select(p => new LedgerProductOptionDto { Id = p.Id, Name = p.Name ?? "Product", Sku = p.Sku });
        return await AsyncExecuter.ToListAsync(q);
    }

    public async Task<LedgerProductDto> GetProductAsync(int productId, int? days)
    {
        var product = await _products.FindAsync(productId) ?? throw new UserFriendlyException("That product no longer exists.");
        var window = days is > 0 ? days.Value : 30;
        var from = Clock.Now.Date.AddDays(-window + 1);

        var all = (await _lines.GetListAsync(l => l.ProductId == productId)).OrderBy(l => l.Id).ToList();
        var inRange = all.Where(l => l.Time >= from).ToList();
        var balances = await _balances.GetListAsync(b => b.ProductId == productId);
        var quantity = balances.Sum(b => b.Quantity);
        var value = balances.Sum(b => b.Quantity * b.AvgCost);
        var corrections = inRange.Where(l => IsHandCorrection(l.Movement)).ToList();

        var dto = new LedgerProductDto
        {
            Product = new LedgerProductOptionDto { Id = product.Id, Name = product.Name ?? "Product", Sku = product.Sku },
            TracksSerials = await _serials.AnyAsync(s => s.ProductId == productId),
            InStockNow = quantity,
            WhereText = string.Join(", ", balances.Where(b => b.Quantity != 0).OrderByDescending(b => b.Quantity)
                .Select(b => $"{b.Quantity} {WarehouseNameOf(all, b.WarehouseId)}")),
            LinesInRange = inRange.Count,
            MovesIn = inRange.Count(l => l.Change > 0),
            MovesOut = inRange.Count(l => l.Change < 0),
            HandCorrections = corrections.Count,
            HandCorrectionsBy = corrections.Select(c => c.UserName).Distinct().Count() == 1 ? corrections[0].UserName : null,
            StockValue = Math.Round(value, 2),
            AverageCost = quantity > 0 ? Math.Round(value / quantity, 2) : 0,
            Lines = await MapLinesAsync(all.OrderByDescending(l => l.Id).Take(25).ToList()),
            Balance = BalanceOverTime(all, from, quantity),
            Receipts = all.Where(l => l.Change > 0).OrderByDescending(l => l.Id).Take(6).Select(l => new LedgerCostLayerDto
            {
                LineId = l.Id, Date = l.Time, Title = $"{MovementLabel(l.Movement)} {l.Time:d MMM}",
                Detail = l.DocumentNumber, Quantity = l.Change, UnitCost = l.UnitCost,
            }).ToList(),
        };
        return dto;
    }

    private static string WarehouseNameOf(List<StockLedgerLine> lines, int warehouseId) =>
        lines.FirstOrDefault(l => l.WarehouseId == warehouseId)?.WarehouseName ?? $"warehouse {warehouseId}";

    /// <summary>
    /// The balance at the end of each day, worked back from today's quantity through the lines, so the
    /// curve always ends where the stock actually is.
    /// </summary>
    private List<LedgerBalancePointDto> BalanceOverTime(List<StockLedgerLine> all, DateTime from, int today)
    {
        var points = new List<LedgerBalancePointDto>();
        var lastDay = Clock.Now.Date;
        var running = today;
        var byDay = all.GroupBy(l => l.Time.Date).ToDictionary(g => g.Key, g => g.ToList());

        for (var day = lastDay; day >= from; day = day.AddDays(-1))
        {
            var moved = byDay.GetValueOrDefault(day);
            points.Add(new LedgerBalancePointDto
            {
                Date = day, Quantity = running,
                In = moved?.Where(l => l.Change > 0).Sum(l => l.Change) ?? 0,
                Out = moved?.Where(l => l.Change < 0).Sum(l => -l.Change) ?? 0,
            });
            if (moved != null) running -= moved.Sum(l => l.Change);
        }
        points.Reverse();
        return points;
    }
}
