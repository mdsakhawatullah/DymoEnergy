using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Stock;

[Authorize(DymoEnergyPermissions.Stock.Default)]
public class StockEntryAppService : ApplicationService, IStockEntryAppService
{
    private readonly IRepository<StockEntry, int>           _entries;
    private readonly IRepository<StockEntryLine, int>       _lines;
    private readonly IRepository<StockEntryAttachment, int> _attachments;
    private readonly IRepository<StockBalance, int>         _balances;
    private readonly IRepository<StockSupplier, int>        _suppliers;
    private readonly IRepository<Warehouse, int>            _warehouses;
    private readonly IRepository<Product, int>              _products;
    private readonly StockLedger _ledger;

    public StockEntryAppService(
        IRepository<StockEntry, int> entries, IRepository<StockEntryLine, int> lines, IRepository<StockEntryAttachment, int> attachments,
        IRepository<StockBalance, int> balances, IRepository<StockSupplier, int> suppliers, IRepository<Warehouse, int> warehouses,
        IRepository<Product, int> products, StockLedger ledger)
    {
        _entries = entries; _lines = lines; _attachments = attachments; _balances = balances; _suppliers = suppliers;
        _warehouses = warehouses; _products = products; _ledger = ledger;
    }

    public static string ReasonLabel(StockOutReason? r) => r switch
    {
        StockOutReason.Sold => "Sold",
        StockOutReason.Installed => "Used in installation",
        StockOutReason.Damaged => "Damaged",
        StockOutReason.ReturnedToSupplier => "Returned to supplier",
        StockOutReason.Lost => "Lost or stolen",
        StockOutReason.InternalUse => "Used internally",
        _ => "Other",
    };

    // ══ OVERVIEW ═════════════════════════════════════════════════════════════

    public async Task<StockOverviewDto> GetOverviewAsync()
    {
        var warehouses = await _ledger.WarehousesAsync();
        var products = await AsyncExecuter.ToListAsync((await _products.GetQueryableAsync())
            .Select(p => new { p.Id, p.Name, p.Sku, p.StockQuantity, p.IsActive }));
        var balances = await _balances.GetListAsync();
        var byProduct = balances.GroupBy(b => b.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var dto = new StockOverviewDto();
        var units = new Dictionary<int, int>();
        foreach (var p in products)
        {
            if (byProduct.TryGetValue(p.Id, out var rows))
            {
                var q = rows.Sum(b => Math.Max(0, b.Quantity));
                units[p.Id] = q;
                dto.StockValue += rows.Sum(b => Math.Max(0, b.Quantity) * b.AvgCost);
                if (q > 0 && rows.All(b => b.AvgCost == 0)) dto.ProductsWithoutCost++;
            }
            else
            {
                units[p.Id] = Math.Max(0, p.StockQuantity);
                if (p.StockQuantity > 0) dto.ProductsWithoutCost++;
            }
        }
        dto.StockUnits = units.Values.Sum();
        dto.ProductsInStock = units.Count(u => u.Value > 0);

        var low = products.Where(p => p.IsActive && units[p.Id] <= StockConsts.LowStockThreshold)
            .OrderBy(p => units[p.Id]).ThenBy(p => p.Name).ToList();
        dto.LowCount = low.Count;
        dto.Restock = low.Take(8).Select(p => new RestockItemDto { ProductId = p.Id, Name = p.Name ?? $"#{p.Id}", Sku = p.Sku, Left = units[p.Id] }).ToList();

        // This month, counting only entries that still stand (not reversed, not reversals).
        var monthStart = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        var month = await _entries.GetListAsync(e => e.Status == StockEntryStatus.Posted && e.ReversesId == null && e.Date >= monthStart
            && (e.Type == StockEntryType.StockIn || e.Type == StockEntryType.StockOut));
        var ids = month.Select(e => e.Id).ToList();
        var lines = await _lines.GetListAsync(l => ids.Contains(l.StockEntryId));
        var stockIn = month.Where(e => e.Type == StockEntryType.StockIn).Select(e => e.Id).ToHashSet();
        var inLines = lines.Where(l => stockIn.Contains(l.StockEntryId)).ToList();
        dto.ReceivedEntries = stockIn.Count;
        dto.ReceivedUnits = inLines.Sum(l => l.Quantity);
        dto.ReceivedValue = inLines.Sum(l => l.Quantity * l.LandedUnitCost);

        var reasons = month.Where(e => e.Type == StockEntryType.StockOut).ToDictionary(e => e.Id, e => e.OutReason ?? StockOutReason.Other);
        var outLines = lines.Where(l => reasons.ContainsKey(l.StockEntryId)).ToList();
        dto.OutUnits = outLines.Sum(l => l.Quantity);
        dto.OutParts = outLines.GroupBy(l => reasons[l.StockEntryId]).Select(g => new StockOutPartDto
        {
            Reason = g.Key, Label = g.Key switch
            {
                StockOutReason.Sold => "sold", StockOutReason.Installed => "installed", StockOutReason.Damaged => "damaged",
                StockOutReason.ReturnedToSupplier => "returned", StockOutReason.Lost => "lost", StockOutReason.InternalUse => "used", _ => "other",
            },
            Units = g.Sum(l => l.Quantity),
        }).OrderByDescending(p => p.Units).ToList();

        var whUnits = balances.GroupBy(b => b.WarehouseId).ToDictionary(g => g.Key, g => g.Sum(b => Math.Max(0, b.Quantity)));
        dto.Warehouses = warehouses.Select(w => MapWarehouse(w, whUnits.GetValueOrDefault(w.Id))).ToList();
        dto.Suppliers = (await _suppliers.GetListAsync()).OrderBy(s => s.Name).Select(MapSupplier).ToList();
        return dto;
    }

    // ══ LIST ═════════════════════════════════════════════════════════════════

    public async Task<StockEntriesPageDto> GetEntriesAsync(GetStockEntriesInput input)
    {
        var q = await _entries.GetQueryableAsync();

        if (input.WarehouseId is { } wh) q = q.Where(e => e.WarehouseId == wh || e.ToWarehouseId == wh);
        if (input.Status is { } st) q = q.Where(e => e.Status == st);
        if (input.Days is { } days)
        {
            var from = days == 0 ? new DateTime(Clock.Now.Year, Clock.Now.Month, 1) : Clock.Now.Date.AddDays(-days);
            q = q.Where(e => e.Date >= from);
        }
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLower();
            var supplierIds = await AsyncExecuter.ToListAsync((await _suppliers.GetQueryableAsync()).Where(s => s.Name.ToLower().Contains(f)).Select(s => s.Id));
            var productIds = await AsyncExecuter.ToListAsync((await _products.GetQueryableAsync())
                .Where(p => (p.Name != null && p.Name.ToLower().Contains(f)) || (p.Sku != null && p.Sku.ToLower().Contains(f))).Select(p => p.Id));
            var entryIds = productIds.Count == 0 ? new List<int>() : await AsyncExecuter.ToListAsync((await _lines.GetQueryableAsync())
                .Where(l => productIds.Contains(l.ProductId)).Select(l => l.StockEntryId).Distinct());
            q = q.Where(e => e.Number.ToLower().Contains(f)
                || (e.InvoiceNumber != null && e.InvoiceNumber.ToLower().Contains(f))
                || (e.Reference != null && e.Reference.ToLower().Contains(f))
                || (e.SupplierId != null && supplierIds.Contains(e.SupplierId.Value))
                || entryIds.Contains(e.Id));
        }

        // Tab counts use every filter except the tab itself.
        var counts = await AsyncExecuter.ToListAsync(q.GroupBy(e => new { e.Type, Draft = e.Status == StockEntryStatus.Draft })
            .Select(g => new { g.Key.Type, g.Key.Draft, Count = g.Count() }));
        var dtoCounts = new StockEntryCountsDto
        {
            All = counts.Sum(c => c.Count),
            StockIn = counts.Where(c => c.Type == StockEntryType.StockIn).Sum(c => c.Count),
            StockOut = counts.Where(c => c.Type == StockEntryType.StockOut).Sum(c => c.Count),
            Transfer = counts.Where(c => c.Type == StockEntryType.Transfer).Sum(c => c.Count),
            Adjustment = counts.Where(c => c.Type == StockEntryType.Adjustment).Sum(c => c.Count),
            Drafts = counts.Where(c => c.Draft).Sum(c => c.Count),
        };

        if (input.Drafts) q = q.Where(e => e.Status == StockEntryStatus.Draft);
        else if (input.Type is { } type) q = q.Where(e => e.Type == type);

        var total = await AsyncExecuter.CountAsync(q);
        var page = await AsyncExecuter.ToListAsync(q.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Skip(input.SkipCount).Take(input.MaxResultCount));

        return new StockEntriesPageDto { TotalCount = total, Counts = dtoCounts, Items = await SummariesAsync(page) };
    }

    private async Task<List<StockEntrySummaryDto>> SummariesAsync(List<StockEntry> entries)
    {
        if (entries.Count == 0) return new();
        var ids = entries.Select(e => e.Id).ToList();
        var lines = await _lines.GetListAsync(l => ids.Contains(l.StockEntryId));
        var warehouses = (await _ledger.WarehousesAsync()).ToDictionary(w => w.Id, w => w.Name);
        var supplierIds = entries.Where(e => e.SupplierId != null).Select(e => e.SupplierId!.Value).Distinct().ToList();
        var suppliers = (await _suppliers.GetListAsync(s => supplierIds.Contains(s.Id))).ToDictionary(s => s.Id, s => s.Name);

        // Drafts have not touched stock yet; their numbers are estimated from today's balances.
        var draftProductIds = entries.Where(e => e.Status == StockEntryStatus.Draft)
            .SelectMany(e => lines.Where(l => l.StockEntryId == e.Id).Select(l => l.ProductId)).Distinct().ToList();
        var draftBalances = draftProductIds.Count == 0 ? new List<StockBalance>() : await _ledger.BalancesAsync(draftProductIds);
        string Wh(int? id) => id != null && warehouses.TryGetValue(id.Value, out var n) ? n : "—";

        return entries.Select(e =>
        {
            var mine = lines.Where(l => l.StockEntryId == e.Id).ToList();
            var draft = e.Status == StockEntryStatus.Draft;
            int units;
            decimal value;
            if (draft)
            {
                StockBalance? B(int pid) => draftBalances.FirstOrDefault(b => b.ProductId == pid && b.WarehouseId == e.WarehouseId);
                units = e.Type switch
                {
                    StockEntryType.StockIn => mine.Sum(l => l.Quantity),
                    StockEntryType.StockOut => -mine.Sum(l => l.Quantity),
                    StockEntryType.Transfer => mine.Sum(l => l.Quantity),
                    _ => mine.Sum(l => (l.CountedQuantity ?? 0) - (B(l.ProductId)?.Quantity ?? 0)),
                };
                value = e.Type switch
                {
                    StockEntryType.StockIn => mine.Sum(l => l.Quantity * l.UnitCost) + e.TransportCost,
                    StockEntryType.Adjustment => mine.Sum(l => Math.Abs((l.CountedQuantity ?? 0) - (B(l.ProductId)?.Quantity ?? 0)) * (B(l.ProductId)?.AvgCost ?? 0)),
                    _ => mine.Sum(l => l.Quantity * (B(l.ProductId)?.AvgCost ?? 0)),
                };
            }
            else
            {
                // A transfer does not change the total, so it shows the units moved.
                var transfer = e.Type == StockEntryType.Transfer;
                units = transfer ? mine.Sum(l => l.Quantity) : mine.Sum(l => l.Change);
                value = mine.Sum(l => (transfer ? l.Quantity : Math.Abs(l.Change)) * l.LandedUnitCost);
            }

            var (title, sub) = e.Type switch
            {
                StockEntryType.StockIn => (e.SupplierId != null ? suppliers.GetValueOrDefault(e.SupplierId.Value, "Supplier removed") : "No supplier",
                    Join(Wh(e.WarehouseId), e.InvoiceNumber != null ? "Invoice " + e.InvoiceNumber : "Invoice pending")),
                StockEntryType.StockOut => (ReasonLabel(e.OutReason), Join(Wh(e.WarehouseId), e.Reference)),
                StockEntryType.Transfer => ($"{Wh(e.WarehouseId)} → {Wh(e.ToWarehouseId)}", e.Reference ?? e.Note ?? string.Empty),
                _ => ("Stock count correction", Join(Wh(e.WarehouseId), e.Reference)),
            };
            if (e.ReversesId != null) { sub = Join(e.Reference, sub.Split(" · ")[0]); }

            return new StockEntrySummaryDto
            {
                Id = e.Id, Number = e.Number, Type = e.Type, Status = e.Status, Date = e.Date, Title = title, Subtitle = sub,
                ItemCount = mine.Count, Units = units, Value = Math.Round(value, 2), IsReversal = e.ReversesId != null,
            };
        }).ToList();
    }

    private static string Join(params string?[] parts) => string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct());

    // ══ ONE ENTRY ════════════════════════════════════════════════════════════

    public async Task<StockEntryDto> GetAsync(int id) => await MapEntryAsync(await _entries.GetAsync(id));

    public async Task<string> GetNextNumberAsync() => await _ledger.NextNumberAsync();

    private async Task<StockEntryDto> MapEntryAsync(StockEntry e)
    {
        var lines = (await _lines.GetListAsync(l => l.StockEntryId == e.Id)).OrderBy(l => l.Order).ThenBy(l => l.Id).ToList();
        var pids = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _products.GetListAsync(p => pids.Contains(p.Id))).ToDictionary(p => p.Id);
        var balances = await _ledger.BalancesAsync(pids);
        var tracked = await _ledger.SerialTrackedAsync(pids);
        var warehouses = (await _ledger.WarehousesAsync()).ToDictionary(w => w.Id, w => w.Name);
        var related = new[] { e.ReversesId, e.ReversedById }.Where(x => x != null).Select(x => x!.Value).ToList();
        var numbers = related.Count == 0 ? new Dictionary<int, string>() : (await _entries.GetListAsync(x => related.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Number);
        var draft = e.Status == StockEntryStatus.Draft;

        var dto = new StockEntryDto
        {
            Id = e.Id, Number = e.Number, Type = e.Type, Status = e.Status, Date = e.Date,
            WarehouseId = e.WarehouseId, WarehouseName = warehouses.GetValueOrDefault(e.WarehouseId, "—"),
            ToWarehouseId = e.ToWarehouseId, ToWarehouseName = e.ToWarehouseId != null ? warehouses.GetValueOrDefault(e.ToWarehouseId.Value) : null,
            SupplierId = e.SupplierId, SupplierName = e.SupplierId != null ? (await _suppliers.FindAsync(e.SupplierId.Value))?.Name : null,
            InvoiceNumber = e.InvoiceNumber, PurchaseOrder = e.PurchaseOrder, TransportCost = e.TransportCost, OutReason = e.OutReason,
            Reference = e.Reference, Note = e.Note, PostedAt = e.PostedAt, PostedByName = e.PostedByName,
            ReversesId = e.ReversesId, ReversesNumber = e.ReversesId != null ? numbers.GetValueOrDefault(e.ReversesId.Value) : null,
            ReversedById = e.ReversedById, ReversedByNumber = e.ReversedById != null ? numbers.GetValueOrDefault(e.ReversedById.Value) : null,
            Attachments = (await _attachments.GetListAsync(a => a.StockEntryId == e.Id)).OrderBy(a => a.Id)
                .Select(a => new StockAttachmentDto { Id = a.Id, FileName = a.FileName, Url = a.Url, SizeBytes = a.SizeBytes }).ToList(),
        };

        foreach (var l in lines)
        {
            var p = products.GetValueOrDefault(l.ProductId);
            var now = balances.FirstOrDefault(b => b.ProductId == l.ProductId && b.WarehouseId == e.WarehouseId);
            var cost = draft ? (e.Type == StockEntryType.StockIn ? l.UnitCost : now?.AvgCost ?? 0) : l.LandedUnitCost;
            var moved = draft && e.Type == StockEntryType.Adjustment ? Math.Abs((l.CountedQuantity ?? 0) - (now?.Quantity ?? 0)) : (draft ? l.Quantity : Math.Abs(l.Change));
            if (!draft && e.Type == StockEntryType.Transfer) moved = l.Quantity;
            dto.Lines.Add(new StockEntryLineDto
            {
                Id = l.Id, ProductId = l.ProductId, ProductName = p?.Name ?? "Product removed", Sku = p?.Sku, Image = p?.PrimaryImage,
                Quantity = l.Quantity, CountedQuantity = l.CountedQuantity, UnitCost = l.UnitCost, LandedUnitCost = l.LandedUnitCost,
                Change = l.Change, StockBefore = l.StockBefore, StockAfter = l.StockAfter, ToStockAfter = l.ToStockAfter,
                LineTotal = Math.Round(moved * cost, 2), Serials = StockLedger.SplitSerials(l.Serials),
                InStockNow = now?.Quantity ?? 0, TracksSerials = tracked.Contains(l.ProductId),
            });
        }
        dto.Total = dto.Lines.Sum(l => l.LineTotal) + (draft && e.Type == StockEntryType.StockIn ? e.TransportCost : 0);
        return dto;
    }

    // ══ WRITE ════════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<StockEntryDto> CreateAsync(SaveStockEntryDto input)
    {
        await ValidateAsync(input);
        var entry = new StockEntry { Number = await _ledger.NextNumberAsync(), Status = StockEntryStatus.Draft };
        Apply(entry, input);
        await _entries.InsertAsync(entry, autoSave: true);
        await ReplaceLinesAsync(entry, input);
        return await MapEntryAsync(entry);
    }

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<StockEntryDto> UpdateAsync(int id, SaveStockEntryDto input)
    {
        var entry = await _entries.GetAsync(id);
        if (entry.Status != StockEntryStatus.Draft) throw new UserFriendlyException("Posted entries cannot be changed. Reverse it and post a new one.");
        await ValidateAsync(input);
        Apply(entry, input);
        await _entries.UpdateAsync(entry, autoSave: true);
        await ReplaceLinesAsync(entry, input);
        return await MapEntryAsync(entry);
    }

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task DeleteAsync(int id)
    {
        var entry = await _entries.GetAsync(id);
        if (entry.Status != StockEntryStatus.Draft) throw new UserFriendlyException("Posted entries cannot be deleted. Reverse it instead.");
        await _lines.DeleteAsync(l => l.StockEntryId == id, autoSave: true);
        await _attachments.DeleteAsync(a => a.StockEntryId == id, autoSave: true);
        await _entries.DeleteAsync(entry, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.Stock.Post)]
    public async Task<StockEntryDto> PostAsync(int id)
    {
        var entry = await _entries.GetAsync(id);
        var lines = (await _lines.GetListAsync(l => l.StockEntryId == id)).OrderBy(l => l.Order).ToList();
        var hasPhoto = await _attachments.AnyAsync(a => a.StockEntryId == id);
        await _ledger.PostAsync(entry, lines, CurrentUser.Id, UserName(), hasPhoto, "Stock entry screen → Post");
        return await MapEntryAsync(entry);
    }

    [Authorize(DymoEnergyPermissions.Stock.Post)]
    public async Task<StockEntryDto> ReverseAsync(int id)
    {
        var entry = await _entries.GetAsync(id);
        var lines = (await _lines.GetListAsync(l => l.StockEntryId == id)).OrderBy(l => l.Order).ToList();
        var reversal = await _ledger.ReverseAsync(entry, lines, CurrentUser.Id, UserName(), cameFrom: "Stock entry screen → Reverse");
        return await MapEntryAsync(reversal);
    }

    private string? UserName()
    {
        var full = string.Join(" ", new[] { CurrentUser.Name, CurrentUser.SurName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(full) ? CurrentUser.UserName : full;
    }

    private async Task ValidateAsync(SaveStockEntryDto input)
    {
        if (!Enum.IsDefined(input.Type)) throw new UserFriendlyException("Choose what kind of entry this is.");
        if (input.Lines.Count > StockConsts.MaxLines) throw new UserFriendlyException($"An entry can have at most {StockConsts.MaxLines} products.");
        if (!await _warehouses.AnyAsync(w => w.Id == input.WarehouseId)) throw new UserFriendlyException("Choose a warehouse.");
        if (input.Type == StockEntryType.Transfer)
        {
            if (input.ToWarehouseId == null || !await _warehouses.AnyAsync(w => w.Id == input.ToWarehouseId))
                throw new UserFriendlyException("Choose where the stock is moving to.");
            if (input.ToWarehouseId == input.WarehouseId) throw new UserFriendlyException("A transfer needs two different warehouses.");
        }
        if (input.SupplierId != null && !await _suppliers.AnyAsync(s => s.Id == input.SupplierId)) throw new UserFriendlyException("That supplier does not exist.");
        if (input.Lines.GroupBy(l => l.ProductId).Any(g => g.Count() > 1)) throw new UserFriendlyException("Each product can be on the entry once. Change its quantity instead.");

        var pids = input.Lines.Select(l => l.ProductId).ToList();
        var known = (await AsyncExecuter.ToListAsync((await _products.GetQueryableAsync()).Where(p => pids.Contains(p.Id)).Select(p => new { p.Id, p.Name })))
            .ToDictionary(p => p.Id, p => p.Name ?? $"#{p.Id}");
        foreach (var l in input.Lines)
        {
            if (!known.TryGetValue(l.ProductId, out var name)) throw new UserFriendlyException("One of the products no longer exists.");
            var serials = Clean(l.Serials);
            if (serials.Any(s => s.Length > StockConsts.SerialMaxLength)) throw new UserFriendlyException($"{name}: serial numbers can be at most {StockConsts.SerialMaxLength} characters.");
            if (serials.Count != l.Serials.Count(s => !string.IsNullOrWhiteSpace(s))) throw new UserFriendlyException($"{name}: the same serial number is listed twice.");
            if (input.Type != StockEntryType.Adjustment && serials.Count > l.Quantity)
                throw new UserFriendlyException($"{name}: {serials.Count} serial numbers for {l.Quantity} units.");
        }
    }

    private static List<string> Clean(List<string> serials) =>
        serials.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static void Apply(StockEntry e, SaveStockEntryDto i)
    {
        string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        e.Type = i.Type;
        e.Date = i.Date.Date;
        e.WarehouseId = i.WarehouseId;
        e.ToWarehouseId = i.Type == StockEntryType.Transfer ? i.ToWarehouseId : null;
        e.SupplierId = i.Type == StockEntryType.StockIn ? i.SupplierId : null;
        e.InvoiceNumber = i.Type == StockEntryType.StockIn ? T(i.InvoiceNumber) : null;
        e.PurchaseOrder = i.Type == StockEntryType.StockIn ? T(i.PurchaseOrder) : null;
        e.TransportCost = i.Type == StockEntryType.StockIn ? Math.Round(i.TransportCost, 2) : 0;
        e.OutReason = i.Type == StockEntryType.StockOut ? i.OutReason ?? StockOutReason.Other : null;
        e.Reference = T(i.Reference);
        e.Note = T(i.Note);
    }

    private async Task ReplaceLinesAsync(StockEntry entry, SaveStockEntryDto input)
    {
        await _lines.DeleteAsync(l => l.StockEntryId == entry.Id, autoSave: true);
        var order = 0;
        await _lines.InsertManyAsync(input.Lines.Select(l => new StockEntryLine
        {
            StockEntryId = entry.Id, ProductId = l.ProductId, Order = ++order,
            Quantity = input.Type == StockEntryType.Adjustment ? 0 : l.Quantity,
            CountedQuantity = input.Type == StockEntryType.Adjustment ? l.CountedQuantity ?? 0 : null,
            UnitCost = input.Type == StockEntryType.StockIn ? Math.Round(l.UnitCost, 2) : 0,
            Serials = input.Type == StockEntryType.Adjustment ? null : string.Join('\n', Clean(l.Serials)) is { Length: > 0 } s ? s : null,
        }), autoSave: true);
    }

    // ══ ATTACHMENTS ══════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<StockAttachmentDto> AddAttachmentAsync(int id, AddStockAttachmentDto input)
    {
        await _entries.GetAsync(id);
        var a = await _attachments.InsertAsync(new StockEntryAttachment
        {
            StockEntryId = id, FileName = input.FileName.Trim(), Url = input.Url.Trim(), SizeBytes = input.SizeBytes,
        }, autoSave: true);
        return new StockAttachmentDto { Id = a.Id, FileName = a.FileName, Url = a.Url, SizeBytes = a.SizeBytes };
    }

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task DeleteAttachmentAsync(int attachmentId) => await _attachments.DeleteAsync(attachmentId, autoSave: true);

    // ══ PRODUCT PICKER ═══════════════════════════════════════════════════════

    public async Task<List<StockProductDto>> GetProductsAsync(GetStockProductsInput input)
    {
        var q = await _products.GetQueryableAsync();
        List<Product> found;
        if (input.Ids is { Count: > 0 } ids)
        {
            found = await AsyncExecuter.ToListAsync(q.Where(p => ids.Contains(p.Id)));
        }
        else
        {
            var f = (input.Filter ?? string.Empty).Trim().ToLower();
            if (f.Length > 0)
                q = q.Where(p => (p.Name != null && p.Name.ToLower().Contains(f)) || (p.Sku != null && p.Sku.ToLower().Contains(f)));
            found = await AsyncExecuter.ToListAsync(q
                .OrderBy(p => p.Sku != null && p.Sku.ToLower() == f ? 0 : 1)   // a scanned SKU comes first
                .ThenByDescending(p => p.IsActive).ThenBy(p => p.Name).Take(input.MaxResultCount));
        }

        var pids = found.Select(p => p.Id).ToList();
        var balances = await _ledger.BalancesAsync(pids);
        var tracked = await _ledger.SerialTrackedAsync(pids);
        var warehouseId = input.WarehouseId ?? (await _ledger.DefaultWarehouseAsync()).Id;

        var lastCosts = await AsyncExecuter.ToListAsync(
            from l in await _lines.GetQueryableAsync()
            join e in await _entries.GetQueryableAsync() on l.StockEntryId equals e.Id
            where pids.Contains(l.ProductId) && e.Type == StockEntryType.StockIn && e.Status != StockEntryStatus.Draft && e.ReversesId == null
            select new { l.ProductId, l.UnitCost, e.Date, l.Id });
        var last = lastCosts.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).First().UnitCost);

        return found.Select(p =>
        {
            var mine = balances.Where(b => b.ProductId == p.Id).ToList();
            var here = mine.FirstOrDefault(b => b.WarehouseId == warehouseId);
            var totalQty = mine.Sum(b => b.Quantity);
            return new StockProductDto
            {
                Id = p.Id, Name = p.Name ?? $"#{p.Id}", Sku = p.Sku, Image = p.PrimaryImage, InStock = here?.Quantity ?? 0, Total = totalQty,
                AvgCost = here?.AvgCost ?? (totalQty > 0 ? Math.Round(mine.Sum(b => b.Quantity * b.AvgCost) / totalQty, 2) : 0),
                LastCost = last.TryGetValue(p.Id, out var c) ? c : null, TracksSerials = tracked.Contains(p.Id),
            };
        }).ToList();
    }

    // ══ WAREHOUSES & SUPPLIERS ═══════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<WarehouseDto> CreateWarehouseAsync(CreateUpdateWarehouseDto input)
    {
        var all = await _ledger.WarehousesAsync();
        var w = new Warehouse { Order = all.Select(x => x.Order).DefaultIfEmpty(0).Max() + 1 };
        await ApplyWarehouseAsync(w, input, all);
        await _warehouses.InsertAsync(w, autoSave: true);
        return MapWarehouse(w, 0);
    }

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<WarehouseDto> UpdateWarehouseAsync(int id, CreateUpdateWarehouseDto input)
    {
        var all = await _ledger.WarehousesAsync();
        var w = all.FirstOrDefault(x => x.Id == id) ?? throw new UserFriendlyException("That warehouse does not exist.");
        if (w.IsDefault && !input.IsDefault) throw new UserFriendlyException("Make another warehouse the default instead.");
        if (!input.IsActive && w.IsActive && await _balances.AnyAsync(b => b.WarehouseId == id && b.Quantity != 0))
            throw new UserFriendlyException($"{w.Name} still has stock. Move it out before switching the warehouse off.");
        await ApplyWarehouseAsync(w, input, all);
        await _warehouses.UpdateAsync(w, autoSave: true);
        var units = (await _balances.GetListAsync(b => b.WarehouseId == id)).Sum(b => b.Quantity);
        return MapWarehouse(w, units);
    }

    private async Task ApplyWarehouseAsync(Warehouse w, CreateUpdateWarehouseDto i, List<Warehouse> all)
    {
        if (all.Any(x => x.Id != w.Id && string.Equals(x.Name, i.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new UserFriendlyException($"There is already a warehouse called {i.Name.Trim()}.");
        w.Name = i.Name.Trim(); w.ShortCode = i.ShortCode.Trim().ToUpperInvariant(); w.Address = string.IsNullOrWhiteSpace(i.Address) ? null : i.Address.Trim();
        w.IsActive = i.IsActive || i.IsDefault;
        if (i.Order > 0) w.Order = i.Order;
        if (i.IsDefault && !w.IsDefault)
        {
            foreach (var other in all.Where(x => x.IsDefault && x.Id != w.Id)) { other.IsDefault = false; await _warehouses.UpdateAsync(other, autoSave: true); }
            w.IsDefault = true;
        }
    }

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<StockSupplierDto> CreateSupplierAsync(CreateUpdateStockSupplierDto input)
    {
        var name = input.Name.Trim();
        if (await _suppliers.AnyAsync(s => s.Name.ToLower() == name.ToLower())) throw new UserFriendlyException($"{name} is already a supplier.");
        var s = new StockSupplier();
        ApplySupplier(s, input);
        await _suppliers.InsertAsync(s, autoSave: true);
        return MapSupplier(s);
    }

    [Authorize(DymoEnergyPermissions.Stock.Edit)]
    public async Task<StockSupplierDto> UpdateSupplierAsync(int id, CreateUpdateStockSupplierDto input)
    {
        var s = await _suppliers.GetAsync(id);
        ApplySupplier(s, input);
        await _suppliers.UpdateAsync(s, autoSave: true);
        return MapSupplier(s);
    }

    private static void ApplySupplier(StockSupplier s, CreateUpdateStockSupplierDto i)
    {
        string? T(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        s.Name = i.Name.Trim(); s.Phone = T(i.Phone); s.Email = T(i.Email); s.Address = T(i.Address); s.Note = T(i.Note); s.IsActive = i.IsActive;
    }

    private static WarehouseDto MapWarehouse(Warehouse w, int units) => new()
    {
        Id = w.Id, Name = w.Name, ShortCode = w.ShortCode, Address = w.Address, IsDefault = w.IsDefault, IsActive = w.IsActive, Order = w.Order, Units = units,
    };

    private static StockSupplierDto MapSupplier(StockSupplier s) => new()
    {
        Id = s.Id, Name = s.Name, Phone = s.Phone, Email = s.Email, Address = s.Address, Note = s.Note, IsActive = s.IsActive,
    };
}
