using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Data;

namespace DymoEnergy.Stock;

public class Warehouse : FullAuditedAggregateRoot<int>
{
    public string  Name      { get; set; } = string.Empty;
    public string  ShortCode { get; set; } = string.Empty;
    public string? Address   { get; set; }
    /// <summary>Stock that existed before stock entries is treated as sitting here.</summary>
    public bool    IsDefault { get; set; }
    public bool    IsActive  { get; set; } = true;
    public int     Order     { get; set; }
}

public class StockSupplier : FullAuditedAggregateRoot<int>
{
    public string  Name    { get; set; } = string.Empty;
    public string? Phone   { get; set; }
    public string? Email   { get; set; }
    public string? Address { get; set; }
    public string? Note    { get; set; }
    public bool    IsActive { get; set; } = true;
}

/// <summary>Units of one product in one warehouse, and what they cost on average.</summary>
public class StockBalance : Entity<int>, IHasConcurrencyStamp
{
    /// <summary>Two people posting at once cannot both change the same balance.</summary>
    public string  ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    public int     ProductId   { get; set; }
    public int     WarehouseId { get; set; }
    public int     Quantity    { get; set; }
    /// <summary>Moving average landed cost per unit (purchase price + share of transport).</summary>
    public decimal AvgCost     { get; set; }
}

public class StockEntry : FullAuditedAggregateRoot<int>
{
    /// <summary>SE-2026-0143. Given when the entry is first saved, drafts included.</summary>
    public string   Number   { get; set; } = string.Empty;
    public StockEntryType   Type   { get; set; }
    public StockEntryStatus Status { get; set; } = StockEntryStatus.Draft;
    public DateTime Date     { get; set; }

    /// <summary>Stock in / adjustment: the warehouse. Stock out / transfer: where it leaves from.</summary>
    public int      WarehouseId   { get; set; }
    /// <summary>Transfer only: where it arrives.</summary>
    public int?     ToWarehouseId { get; set; }

    public int?     SupplierId    { get; set; }
    public string?  InvoiceNumber { get; set; }
    public string?  PurchaseOrder { get; set; }
    public decimal  TransportCost { get; set; }

    public StockOutReason? OutReason { get; set; }
    /// <summary>Free text: quote or order number, count name, why it was moved.</summary>
    public string?  Reference { get; set; }
    public string?  Note      { get; set; }

    public DateTime? PostedAt     { get; set; }
    public Guid?     PostedBy     { get; set; }
    public string?   PostedByName { get; set; }

    /// <summary>On a reversing entry: the entry it undoes.</summary>
    public int?     ReversesId    { get; set; }
    /// <summary>On a reversed entry: the entry that undid it.</summary>
    public int?     ReversedById  { get; set; }
}

public class StockEntryLine : Entity<int>
{
    public int     StockEntryId { get; set; }
    public int     ProductId    { get; set; }
    public int     Order        { get; set; }
    /// <summary>Units moved, always positive. For adjustments see <see cref="CountedQuantity"/>.</summary>
    public int     Quantity     { get; set; }
    /// <summary>Adjustment only: what was counted. The change is worked out when the entry is posted.</summary>
    public int?    CountedQuantity { get; set; }
    /// <summary>Stock in: the supplier price. Other types: the average cost when posted.</summary>
    public decimal UnitCost     { get; set; }
    /// <summary>Stock in: unit cost plus its share of the transport cost, filled when posted.</summary>
    public decimal LandedUnitCost { get; set; }

    /// <summary>Change in stock when posted, signed (+ in, − out). Zero for transfers' total.</summary>
    public int     Change       { get; set; }
    /// <summary>Units in the (source) warehouse before and after posting.</summary>
    public int?    StockBefore  { get; set; }
    public int?    StockAfter   { get; set; }
    /// <summary>Transfer only: units in the receiving warehouse after posting.</summary>
    public int?    ToStockAfter { get; set; }

    /// <summary>One serial per line.</summary>
    public string? Serials      { get; set; }
}

public class StockEntryAttachment : Entity<int>
{
    public int     StockEntryId { get; set; }
    public string  FileName     { get; set; } = string.Empty;
    public string  Url          { get; set; } = string.Empty;
    public long    SizeBytes    { get; set; }
}

/// <summary>A serial number received into stock, so it can be checked later (warranty, genuine-product page).</summary>
public class StockSerial : Entity<int>
{
    public int     ProductId    { get; set; }
    public string  Serial       { get; set; } = string.Empty;
    public int?    WarehouseId  { get; set; }
    /// <summary>False once it went out (sold, installed, damaged…).</summary>
    public bool    InStock      { get; set; } = true;
    public int     InEntryId    { get; set; }
    public int?    OutEntryId   { get; set; }
    public DateTime ReceivedAt  { get; set; }
}
