using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Products;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;

namespace DymoEnergy.Stock;

/// <summary>
/// The only place that changes stock. Keeps the per-warehouse balances, the moving average cost,
/// the serial numbers and <see cref="Product.StockQuantity"/> (the total) in step.
/// </summary>
public class StockLedger : ITransientDependency
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    private readonly IRepository<StockBalance, int>   _balances;
    private readonly IRepository<Warehouse, int>      _warehouses;
    private readonly IRepository<StockSerial, int>    _serials;
    private readonly IRepository<StockEntry, int>     _entries;
    private readonly IRepository<StockEntryLine, int> _lines;
    private readonly IRepository<Product, int>        _products;
    private readonly IAsyncQueryableExecuter _async;
    private readonly IDataFilter _dataFilter;
    private readonly IClock _clock;

    public StockLedger(
        IRepository<StockBalance, int> balances, IRepository<Warehouse, int> warehouses, IRepository<StockSerial, int> serials,
        IRepository<StockEntry, int> entries, IRepository<StockEntryLine, int> lines, IRepository<Product, int> products,
        IAsyncQueryableExecuter async, IDataFilter dataFilter, IClock clock)
    {
        _balances = balances; _warehouses = warehouses; _serials = serials; _entries = entries; _lines = lines; _products = products;
        _async = async; _dataFilter = dataFilter; _clock = clock;
    }

    // ── Warehouses ──────────────────────────────────────────────────────────

    /// <summary>Creates the two starting warehouses the first time stock is used.</summary>
    public async Task<List<Warehouse>> WarehousesAsync()
    {
        var list = await _warehouses.GetListAsync();
        if (list.Count > 0) return list.OrderBy(w => w.Order).ThenBy(w => w.Id).ToList();

        await SeedLock.WaitAsync();
        try
        {
            list = await _warehouses.GetListAsync();
            if (list.Count == 0)
            {
                await _warehouses.InsertManyAsync(new[]
                {
                    new Warehouse { Name = "Chattogram", ShortCode = "CTG", IsDefault = true, Order = 1 },
                    new Warehouse { Name = "Dhaka", ShortCode = "DHK", Order = 2 },
                }, autoSave: true);
                list = await _warehouses.GetListAsync();
            }
        }
        finally
        {
            SeedLock.Release();
        }
        return list.OrderBy(w => w.Order).ThenBy(w => w.Id).ToList();
    }

    public async Task<Warehouse> DefaultWarehouseAsync()
    {
        var list = await WarehousesAsync();
        return list.FirstOrDefault(w => w.IsDefault) ?? list.First();
    }

    // ── Reading stock ───────────────────────────────────────────────────────

    /// <summary>
    /// Balances for these products. A product that has never been through a stock entry gets its
    /// current <see cref="Product.StockQuantity"/> as an opening balance in the default warehouse.
    /// </summary>
    public async Task<List<StockBalance>> BalancesAsync(ICollection<int> productIds)
    {
        if (productIds.Count == 0) return new();
        var ids = productIds.Distinct().ToList();
        var have = await _balances.GetListAsync(b => ids.Contains(b.ProductId));
        var missing = ids.Except(have.Select(b => b.ProductId)).ToList();
        if (missing.Count > 0)
        {
            var def = await DefaultWarehouseAsync();
            var products = await _products.GetListAsync(p => missing.Contains(p.Id));
            var opening = products.Select(p => new StockBalance { ProductId = p.Id, WarehouseId = def.Id, Quantity = Math.Max(0, p.StockQuantity) }).ToList();
            if (opening.Count > 0)
            {
                await _balances.InsertManyAsync(opening, autoSave: true);
                have.AddRange(opening);
            }
        }
        return have;
    }

    /// <summary>Products that have had serial numbers recorded at least once.</summary>
    public async Task<HashSet<int>> SerialTrackedAsync(ICollection<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        var q = (await _serials.GetQueryableAsync()).Where(s => ids.Contains(s.ProductId)).Select(s => s.ProductId).Distinct();
        return (await _async.ToListAsync(q)).ToHashSet();
    }

    // ── Numbers ─────────────────────────────────────────────────────────────

    /// <summary>SE-2026-0143. Deleted drafts keep their number so a number is never given twice.</summary>
    public async Task<string> NextNumberAsync()
    {
        var prefix = $"SE-{_clock.Now.Year}-";
        List<string> numbers;
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var q = (await _entries.GetQueryableAsync()).Where(e => e.Number.StartsWith(prefix)).Select(e => e.Number);
            numbers = await _async.ToListAsync(q);
        }
        var max = numbers.Select(n => int.TryParse(n[prefix.Length..], out var v) ? v : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{max + 1:D4}";
    }

    // ── Posting ─────────────────────────────────────────────────────────────

    /// <summary>Applies a draft to stock and fills in what each line did. Throws without changing anything when it cannot be done.</summary>
    public async Task PostAsync(StockEntry entry, List<StockEntryLine> lines, Guid? userId, string? userName)
    {
        if (entry.Status != StockEntryStatus.Draft) throw new UserFriendlyException("Only drafts can be posted.");
        if (lines.Count == 0) throw new UserFriendlyException("Add at least one product before posting.");

        var productIds = lines.Select(l => l.ProductId).ToList();
        var balances = await BalancesAsync(productIds);
        var names = await ProductNamesAsync(productIds);
        var warehouses = (await WarehousesAsync()).ToDictionary(w => w.Id);
        string Wh(int id) => warehouses.TryGetValue(id, out var w) ? w.Name : "the warehouse";

        StockBalance Bal(int productId, int warehouseId)
        {
            var b = balances.FirstOrDefault(x => x.ProductId == productId && x.WarehouseId == warehouseId);
            if (b != null) return b;
            b = new StockBalance { ProductId = productId, WarehouseId = warehouseId };
            balances.Add(b);
            return b;
        }

        // Transport is shared by line value, or by units when no line has a price.
        var lineValue = lines.Sum(l => l.Quantity * l.UnitCost);
        var lineUnits = lines.Sum(l => l.Quantity);

        // Nothing is saved until every line and serial checks out; a refusal puts the in-memory numbers back.
        var restore = Snapshot(balances, lines);
        try
        {
        foreach (var line in lines)
        {
            var name = names.GetValueOrDefault(line.ProductId, "a product");
            var from = Bal(line.ProductId, entry.WarehouseId);
            line.StockBefore = from.Quantity;

            switch (entry.Type)
            {
                case StockEntryType.StockIn:
                {
                    if (line.Quantity <= 0) throw new UserFriendlyException($"{name}: the quantity must be more than 0.");
                    var share = entry.TransportCost <= 0 ? 0m
                        : lineValue > 0 ? entry.TransportCost * (line.Quantity * line.UnitCost) / lineValue
                        : entry.TransportCost * line.Quantity / Math.Max(1, lineUnits);
                    line.LandedUnitCost = Math.Round(line.UnitCost + share / line.Quantity, 2);
                    Add(from, line.Quantity, line.LandedUnitCost);
                    line.Change = line.Quantity;
                    break;
                }
                case StockEntryType.StockOut:
                    if (line.Quantity <= 0) throw new UserFriendlyException($"{name}: the quantity must be more than 0.");
                    if (from.Quantity < line.Quantity)
                        throw new UserFriendlyException($"{name}: only {from.Quantity} in {Wh(entry.WarehouseId)}, cannot take out {line.Quantity}.");
                    line.UnitCost = line.LandedUnitCost = from.AvgCost;
                    from.Quantity -= line.Quantity;
                    line.Change = -line.Quantity;
                    break;

                case StockEntryType.Transfer:
                {
                    if (line.Quantity <= 0) throw new UserFriendlyException($"{name}: the quantity must be more than 0.");
                    if (entry.ToWarehouseId is not { } toId || toId == entry.WarehouseId)
                        throw new UserFriendlyException("Choose two different warehouses for a transfer.");
                    if (from.Quantity < line.Quantity)
                        throw new UserFriendlyException($"{name}: only {from.Quantity} in {Wh(entry.WarehouseId)}, cannot move {line.Quantity}.");
                    line.UnitCost = line.LandedUnitCost = from.AvgCost;
                    from.Quantity -= line.Quantity;
                    var to = Bal(line.ProductId, toId);
                    Add(to, line.Quantity, from.AvgCost);
                    line.ToStockAfter = to.Quantity;
                    line.Change = -line.Quantity;   // for the source warehouse; the total does not change
                    break;
                }
                case StockEntryType.Adjustment:
                {
                    if (line.CountedQuantity is not { } counted || counted < 0)
                        throw new UserFriendlyException($"{name}: enter the counted quantity.");
                    line.Change = counted - from.Quantity;
                    line.Quantity = Math.Abs(line.Change);
                    line.UnitCost = line.LandedUnitCost = from.AvgCost;
                    from.Quantity = counted;
                    break;
                }
            }
            line.StockAfter = from.Quantity;
        }

        await ApplySerialsAsync(entry, lines, names, Wh);
        }
        catch
        {
            restore();
            throw;
        }

        entry.Status = StockEntryStatus.Posted;
        entry.PostedAt = _clock.Now;
        entry.PostedBy = userId;
        entry.PostedByName = userName;

        await SaveBalancesAsync(balances);
        await _lines.UpdateManyAsync(lines, autoSave: true);
        await _entries.UpdateAsync(entry, autoSave: true);
        await SyncProductTotalsAsync(productIds);
    }

    /// <summary>Creates and posts the entry that undoes <paramref name="entry"/>.</summary>
    public async Task<StockEntry> ReverseAsync(StockEntry entry, List<StockEntryLine> lines, Guid? userId, string? userName)
    {
        if (entry.Status != StockEntryStatus.Posted) throw new UserFriendlyException("Only posted entries can be reversed.");
        if (entry.ReversesId != null) throw new UserFriendlyException("This entry already undoes another one. Post a new entry instead.");

        var productIds = lines.Select(l => l.ProductId).ToList();
        var balances = await BalancesAsync(productIds);
        var names = await ProductNamesAsync(productIds);
        var warehouses = (await WarehousesAsync()).ToDictionary(w => w.Id);
        string Wh(int id) => warehouses.TryGetValue(id, out var w) ? w.Name : "the warehouse";

        StockBalance Bal(int productId, int warehouseId)
        {
            var b = balances.FirstOrDefault(x => x.ProductId == productId && x.WarehouseId == warehouseId);
            if (b != null) return b;
            b = new StockBalance { ProductId = productId, WarehouseId = warehouseId };
            balances.Add(b);
            return b;
        }

        var reversal = new StockEntry
        {
            Number = await NextNumberAsync(), Type = entry.Type, Status = StockEntryStatus.Posted, Date = _clock.Now.Date,
            WarehouseId = entry.Type == StockEntryType.Transfer ? entry.ToWarehouseId!.Value : entry.WarehouseId,
            ToWarehouseId = entry.Type == StockEntryType.Transfer ? entry.WarehouseId : null,
            SupplierId = entry.SupplierId, InvoiceNumber = entry.InvoiceNumber, PurchaseOrder = entry.PurchaseOrder,
            OutReason = entry.OutReason, Reference = $"Reverses {entry.Number}", ReversesId = entry.Id,
            PostedAt = _clock.Now, PostedBy = userId, PostedByName = userName,
        };

        var newLines = new List<StockEntryLine>();
        var restore = Snapshot(balances, lines);
        try
        {
        foreach (var line in lines)
        {
            var name = names.GetValueOrDefault(line.ProductId, "a product");
            var nl = new StockEntryLine
            {
                ProductId = line.ProductId, Order = line.Order, Quantity = line.Quantity,
                UnitCost = line.UnitCost, LandedUnitCost = line.LandedUnitCost, Serials = line.Serials,
            };
            var b = Bal(line.ProductId, reversal.WarehouseId);
            nl.StockBefore = b.Quantity;

            switch (entry.Type)
            {
                case StockEntryType.StockIn:
                    if (b.Quantity < line.Quantity)
                        throw new UserFriendlyException($"{name}: only {b.Quantity} left in {Wh(b.WarehouseId)} — some of what this entry received has already gone out. Take those back first.");
                    Remove(b, line.Quantity, line.LandedUnitCost);
                    nl.Change = -line.Quantity;
                    break;
                case StockEntryType.StockOut:
                    Add(b, line.Quantity, line.UnitCost);
                    nl.Change = line.Quantity;
                    break;
                case StockEntryType.Transfer:
                {
                    if (b.Quantity < line.Quantity)
                        throw new UserFriendlyException($"{name}: only {b.Quantity} left in {Wh(b.WarehouseId)}, cannot move {line.Quantity} back.");
                    b.Quantity -= line.Quantity;
                    var to = Bal(line.ProductId, reversal.ToWarehouseId!.Value);
                    Add(to, line.Quantity, line.UnitCost);
                    nl.ToStockAfter = to.Quantity;
                    nl.Change = -line.Quantity;
                    break;
                }
                case StockEntryType.Adjustment:
                    if (b.Quantity - line.Change < 0)
                        throw new UserFriendlyException($"{name}: undoing this count would leave {b.Quantity - line.Change} in {Wh(b.WarehouseId)}.");
                    b.Quantity -= line.Change;
                    nl.Change = -line.Change;
                    nl.CountedQuantity = b.Quantity;
                    break;
            }
            nl.StockAfter = b.Quantity;
            newLines.Add(nl);
        }

        await UndoSerialsAsync(entry, lines);
        }
        catch
        {
            restore();
            throw;
        }

        await _entries.InsertAsync(reversal, autoSave: true);
        foreach (var nl in newLines) nl.StockEntryId = reversal.Id;

        entry.Status = StockEntryStatus.Reversed;
        entry.ReversedById = reversal.Id;

        await SaveBalancesAsync(balances);
        await _lines.InsertManyAsync(newLines, autoSave: true);
        await _entries.UpdateAsync(entry, autoSave: true);
        await _entries.UpdateAsync(reversal, autoSave: true);
        await SyncProductTotalsAsync(productIds);
        return reversal;
    }

    /// <summary>
    /// The product form still has a stock box. When someone changes it for a product that is already
    /// tracked by stock entries, the difference is booked as a posted adjustment in the default warehouse
    /// so the balances and the history stay right.
    /// </summary>
    public async Task RecordProductPageChangeAsync(Product product, int oldQuantity, Guid? userId, string? userName)
    {
        var diff = product.StockQuantity - oldQuantity;
        if (diff == 0) return;
        if (!await _balances.AnyAsync(b => b.ProductId == product.Id)) return;   // not tracked yet: the new number is the opening stock

        var def = await DefaultWarehouseAsync();
        var bal = (await BalancesAsync(new[] { product.Id })).FirstOrDefault(b => b.WarehouseId == def.Id);
        var counted = (bal?.Quantity ?? 0) + diff;
        if (counted < 0)
            throw new UserFriendlyException($"{def.Name} only has {bal?.Quantity ?? 0} of this product. Use a stock entry to change stock in other warehouses.");

        var entry = new StockEntry
        {
            Number = await NextNumberAsync(), Type = StockEntryType.Adjustment, Date = _clock.Now.Date,
            WarehouseId = def.Id, Reference = "Changed on the product page",
        };
        await _entries.InsertAsync(entry, autoSave: true);
        var line = new StockEntryLine { StockEntryId = entry.Id, ProductId = product.Id, Order = 1, CountedQuantity = counted };
        await _lines.InsertAsync(line, autoSave: true);
        await PostAsync(entry, new List<StockEntryLine> { line }, userId, userName);
        product.StockQuantity = (await _balances.GetListAsync(b => b.ProductId == product.Id)).Sum(b => b.Quantity);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Remembers the numbers so a refused post or reversal leaves no half-changed entities behind.</summary>
    private static Action Snapshot(List<StockBalance> balances, List<StockEntryLine> lines)
    {
        var count = balances.Count;
        var b = balances.Select(x => (x, x.Quantity, x.AvgCost)).ToList();
        var l = lines.Select(x => (x, x.Quantity, x.UnitCost, x.LandedUnitCost, x.Change, x.StockBefore, x.StockAfter, x.ToStockAfter)).ToList();
        return () =>
        {
            foreach (var s in b) { s.x.Quantity = s.Quantity; s.x.AvgCost = s.AvgCost; }
            balances.RemoveRange(count, balances.Count - count);
            foreach (var s in l)
            {
                s.x.Quantity = s.Quantity; s.x.UnitCost = s.UnitCost; s.x.LandedUnitCost = s.LandedUnitCost; s.x.Change = s.Change;
                s.x.StockBefore = s.StockBefore; s.x.StockAfter = s.StockAfter; s.x.ToStockAfter = s.ToStockAfter;
            }
        };
    }

    private static void Add(StockBalance b, int qty, decimal unitCost)
    {
        var have = Math.Max(0, b.Quantity);
        b.AvgCost = have + qty == 0 ? b.AvgCost : Math.Round((have * b.AvgCost + qty * unitCost) / (have + qty), 2);
        b.Quantity += qty;
    }

    /// <summary>Takes units back out at the cost they came in at, so the average returns to what it was.</summary>
    private static void Remove(StockBalance b, int qty, decimal unitCost)
    {
        var left = b.Quantity - qty;
        if (left > 0) b.AvgCost = Math.Max(0, Math.Round((b.Quantity * b.AvgCost - qty * unitCost) / left, 2));
        b.Quantity = left;
    }

    private async Task SaveBalancesAsync(List<StockBalance> balances)
    {
        var fresh = balances.Where(b => b.Id == 0).ToList();
        var old = balances.Where(b => b.Id != 0).ToList();
        if (fresh.Count > 0) await _balances.InsertManyAsync(fresh, autoSave: true);
        if (old.Count > 0) await _balances.UpdateManyAsync(old, autoSave: true);
    }

    private async Task SyncProductTotalsAsync(List<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        var totals = (await _balances.GetListAsync(b => ids.Contains(b.ProductId)))
            .GroupBy(b => b.ProductId).ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));
        var products = await _products.GetListAsync(p => ids.Contains(p.Id));
        foreach (var p in products) p.StockQuantity = totals.GetValueOrDefault(p.Id);
        await _products.UpdateManyAsync(products, autoSave: true);
    }

    private async Task<Dictionary<int, string>> ProductNamesAsync(List<int> ids)
    {
        var q = (await _products.GetQueryableAsync()).Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Name });
        return (await _async.ToListAsync(q)).ToDictionary(p => p.Id, p => p.Name ?? $"Product #{p.Id}");
    }

    public static List<string> SplitSerials(string? text) =>
        string.IsNullOrWhiteSpace(text) ? new() : text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private async Task ApplySerialsAsync(StockEntry entry, List<StockEntryLine> lines, Dictionary<int, string> names, Func<int, string> wh)
    {
        if (entry.Type == StockEntryType.Adjustment) return;

        // Pass 1: check every line, write nothing.
        var plan = new List<(StockEntryLine Line, List<string> Serials, List<StockSerial> Rows)>();
        foreach (var line in lines)
        {
            var serials = SplitSerials(line.Serials);
            if (serials.Count == 0) continue;
            var name = names.GetValueOrDefault(line.ProductId, "a product");
            var existing = await _serials.GetListAsync(s => s.ProductId == line.ProductId && serials.Contains(s.Serial));
            if (entry.Type == StockEntryType.StockIn)
            {
                var dup = existing.Where(s => s.InStock).Select(s => s.Serial).ToList();
                if (dup.Count > 0) throw new UserFriendlyException($"{name}: {Few(dup)} already in stock.");
                plan.Add((line, serials, existing));
            }
            else
            {
                // Out or transfer: every serial must be on the shelf in the warehouse it leaves from.
                var here = existing.Where(s => s.InStock && s.WarehouseId == entry.WarehouseId).ToList();
                var missing = serials.Except(here.Select(s => s.Serial)).ToList();
                if (missing.Count > 0) throw new UserFriendlyException($"{name}: {Few(missing)} not in stock in {wh(entry.WarehouseId)}.");
                plan.Add((line, serials, here));
            }
        }

        // Pass 2: write.
        foreach (var (line, serials, rows) in plan)
        {
            if (entry.Type == StockEntryType.StockIn)
            {
                foreach (var s in rows)
                {
                    s.InStock = true; s.WarehouseId = entry.WarehouseId; s.InEntryId = entry.Id; s.OutEntryId = null; s.ReceivedAt = entry.Date;
                }
                await _serials.UpdateManyAsync(rows, autoSave: true);
                var known = rows.Select(s => s.Serial).ToHashSet(StringComparer.Ordinal);
                await _serials.InsertManyAsync(serials.Where(s => !known.Contains(s)).Select(s => new StockSerial
                {
                    ProductId = line.ProductId, Serial = s, WarehouseId = entry.WarehouseId, InEntryId = entry.Id, ReceivedAt = entry.Date,
                }), autoSave: true);
            }
            else
            {
                foreach (var s in rows)
                {
                    if (entry.Type == StockEntryType.StockOut) { s.InStock = false; s.OutEntryId = entry.Id; }
                    else s.WarehouseId = entry.ToWarehouseId;
                }
                await _serials.UpdateManyAsync(rows, autoSave: true);
            }
        }
    }

    private async Task UndoSerialsAsync(StockEntry entry, List<StockEntryLine> lines)
    {
        switch (entry.Type)
        {
            case StockEntryType.StockIn:
            {
                var rows = await _serials.GetListAsync(s => s.InEntryId == entry.Id);
                if (rows.Any(s => !s.InStock))
                    throw new UserFriendlyException($"Serial {Few(rows.Where(s => !s.InStock).Select(s => s.Serial).ToList())} from this entry already went out. Take it back first.");
                await _serials.DeleteManyAsync(rows, autoSave: true);
                break;
            }
            case StockEntryType.StockOut:
            {
                var rows = await _serials.GetListAsync(s => s.OutEntryId == entry.Id);
                foreach (var s in rows) { s.InStock = true; s.OutEntryId = null; }
                await _serials.UpdateManyAsync(rows, autoSave: true);
                break;
            }
            case StockEntryType.Transfer:
                foreach (var line in lines)
                {
                    var serials = SplitSerials(line.Serials);
                    if (serials.Count == 0) continue;
                    var rows = await _serials.GetListAsync(s => s.ProductId == line.ProductId && serials.Contains(s.Serial) && s.WarehouseId == entry.ToWarehouseId);
                    foreach (var s in rows) s.WarehouseId = entry.WarehouseId;
                    await _serials.UpdateManyAsync(rows, autoSave: true);
                }
                break;
        }
    }

    private static string Few(List<string> serials) =>
        serials.Count <= 3 ? "serial " + string.Join(", ", serials) + (serials.Count == 1 ? " is" : " are")
                           : $"serials {string.Join(", ", serials.Take(3))} and {serials.Count - 3} more are";
}
