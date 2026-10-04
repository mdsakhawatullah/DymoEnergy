using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Stock;

// ── Warehouses & suppliers ────────────────────────────────────────────────

public class WarehouseDto
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  ShortCode { get; set; } = string.Empty;
    public string? Address   { get; set; }
    public bool    IsDefault { get; set; }
    public bool    IsActive  { get; set; }
    public int     Order     { get; set; }
    public int     Units     { get; set; }
}

public class CreateUpdateWarehouseDto
{
    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(8)]   public string ShortCode { get; set; } = string.Empty;
    [MaxLength(512)] public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive  { get; set; } = true;
    public int  Order     { get; set; }
}

public class StockSupplierDto
{
    public int     Id      { get; set; }
    public string  Name    { get; set; } = string.Empty;
    public string? Phone   { get; set; }
    public string? Email   { get; set; }
    public string? Address { get; set; }
    public string? Note    { get; set; }
    public bool    IsActive { get; set; }
}

public class CreateUpdateStockSupplierDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [MaxLength(32)]   public string? Phone   { get; set; }
    [MaxLength(256)]  public string? Email   { get; set; }
    [MaxLength(512)]  public string? Address { get; set; }
    [MaxLength(2000)] public string? Note    { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Overview (list page header) ───────────────────────────────────────────

public class StockOutPartDto
{
    public StockOutReason Reason { get; set; }
    public string Label { get; set; } = string.Empty;
    public int    Units { get; set; }
}

public class RestockItemDto
{
    public int     ProductId { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string? Sku       { get; set; }
    public int     Left      { get; set; }
}

public class StockOverviewDto
{
    public decimal StockValue        { get; set; }
    public int     StockUnits        { get; set; }
    public int     ProductsInStock   { get; set; }
    /// <summary>Products with units but no known cost (never received through a stock entry).</summary>
    public int     ProductsWithoutCost { get; set; }

    public int     ReceivedUnits     { get; set; }
    public decimal ReceivedValue     { get; set; }
    public int     ReceivedEntries   { get; set; }

    public int     OutUnits          { get; set; }
    public List<StockOutPartDto> OutParts { get; set; } = new();

    public int     LowCount          { get; set; }
    public List<RestockItemDto> Restock { get; set; } = new();

    public List<WarehouseDto>     Warehouses { get; set; } = new();
    public List<StockSupplierDto> Suppliers  { get; set; } = new();
}

// ── Entry list ────────────────────────────────────────────────────────────

public class GetStockEntriesInput
{
    [MaxLength(128)] public string? Filter { get; set; }
    public StockEntryType?   Type   { get; set; }
    /// <summary>Only drafts (the "Drafts" tab).</summary>
    public bool              Drafts { get; set; }
    public StockEntryStatus? Status { get; set; }
    public int?      WarehouseId { get; set; }
    /// <summary>7, 30, 90, 365; 0 = this month; null = all time.</summary>
    public int?      Days        { get; set; }
    public int       SkipCount   { get; set; }
    [Range(1, 1000)] public int MaxResultCount { get; set; } = 8;
}

public class StockEntryCountsDto
{
    public int All        { get; set; }
    public int StockIn    { get; set; }
    public int StockOut   { get; set; }
    public int Transfer   { get; set; }
    public int Adjustment { get; set; }
    public int Drafts     { get; set; }
}

public class StockEntrySummaryDto
{
    public int      Id       { get; set; }
    public string   Number   { get; set; } = string.Empty;
    public StockEntryType   Type   { get; set; }
    public StockEntryStatus Status { get; set; }
    public DateTime Date     { get; set; }
    /// <summary>Supplier name, out reason, "Chattogram → Dhaka" or the count name.</summary>
    public string   Title    { get; set; } = string.Empty;
    /// <summary>Warehouse · invoice / reference.</summary>
    public string   Subtitle { get; set; } = string.Empty;
    public int      ItemCount { get; set; }
    /// <summary>Signed change in stock. Transfers show the units moved (positive).</summary>
    public int      Units    { get; set; }
    public decimal  Value    { get; set; }
    public bool     IsReversal { get; set; }
}

public class StockEntriesPageDto
{
    public long TotalCount { get; set; }
    public List<StockEntrySummaryDto> Items { get; set; } = new();
    public StockEntryCountsDto Counts { get; set; } = new();
}

// ── One entry ─────────────────────────────────────────────────────────────

public class StockEntryLineDto
{
    public int      Id          { get; set; }
    public int      ProductId   { get; set; }
    public string   ProductName { get; set; } = string.Empty;
    public string?  Sku         { get; set; }
    public string?  Image       { get; set; }
    public int      Quantity    { get; set; }
    public int?     CountedQuantity { get; set; }
    public decimal  UnitCost    { get; set; }
    public decimal  LandedUnitCost { get; set; }
    public int      Change      { get; set; }
    public int?     StockBefore { get; set; }
    public int?     StockAfter  { get; set; }
    public int?     ToStockAfter { get; set; }
    public decimal  LineTotal   { get; set; }
    public List<string> Serials { get; set; } = new();
    /// <summary>Units now in the entry's warehouse (live, for drafts).</summary>
    public int      InStockNow  { get; set; }
    public bool     TracksSerials { get; set; }
}

public class StockAttachmentDto
{
    public int    Id        { get; set; }
    public string FileName  { get; set; } = string.Empty;
    public string Url       { get; set; } = string.Empty;
    public long   SizeBytes { get; set; }
}

public class StockEntryDto
{
    public int      Id       { get; set; }
    public string   Number   { get; set; } = string.Empty;
    public StockEntryType   Type   { get; set; }
    public StockEntryStatus Status { get; set; }
    public DateTime Date     { get; set; }
    public int      WarehouseId   { get; set; }
    public string   WarehouseName { get; set; } = string.Empty;
    public int?     ToWarehouseId   { get; set; }
    public string?  ToWarehouseName { get; set; }
    public int?     SupplierId    { get; set; }
    public string?  SupplierName  { get; set; }
    public string?  InvoiceNumber { get; set; }
    public string?  PurchaseOrder { get; set; }
    public decimal  TransportCost { get; set; }
    public StockOutReason? OutReason { get; set; }
    public string?  Reference { get; set; }
    public string?  Note      { get; set; }
    public DateTime? PostedAt { get; set; }
    public string?  PostedByName { get; set; }
    public int?     ReversesId { get; set; }
    public string?  ReversesNumber { get; set; }
    public int?     ReversedById { get; set; }
    public string?  ReversedByNumber { get; set; }
    public decimal  Total     { get; set; }
    public List<StockEntryLineDto>  Lines       { get; set; } = new();
    public List<StockAttachmentDto> Attachments { get; set; } = new();
}

public class SaveStockEntryLineDto
{
    public int ProductId { get; set; }
    [Range(0, 1000000)] public int Quantity { get; set; }
    /// <summary>Adjustment only.</summary>
    [Range(0, 1000000)] public int? CountedQuantity { get; set; }
    [Range(0, 100000000)] public decimal UnitCost { get; set; }
    public List<string> Serials { get; set; } = new();
}

public class SaveStockEntryDto
{
    public StockEntryType Type { get; set; }
    public DateTime Date { get; set; }
    public int  WarehouseId   { get; set; }
    public int? ToWarehouseId { get; set; }
    public int? SupplierId    { get; set; }
    [MaxLength(128)] public string? InvoiceNumber { get; set; }
    [MaxLength(128)] public string? PurchaseOrder { get; set; }
    [Range(0, 100000000)] public decimal TransportCost { get; set; }
    public StockOutReason? OutReason { get; set; }
    [MaxLength(256)]  public string? Reference { get; set; }
    [MaxLength(4000)] public string? Note { get; set; }
    public List<SaveStockEntryLineDto> Lines { get; set; } = new();
}

public class AddStockAttachmentDto
{
    [Required, MaxLength(256)]  public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(1024)] public string Url { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

// ── Product picker ────────────────────────────────────────────────────────

public class GetStockProductsInput
{
    [MaxLength(128)] public string? Filter { get; set; }
    public int? WarehouseId { get; set; }
    /// <summary>Fetch these products (the lines already on the entry) instead of searching.</summary>
    public List<int>? Ids { get; set; }
    [Range(1, 50)] public int MaxResultCount { get; set; } = 12;
}

public class StockProductDto
{
    public int     Id      { get; set; }
    public string  Name    { get; set; } = string.Empty;
    public string? Sku     { get; set; }
    public string? Image   { get; set; }
    /// <summary>Units in the requested warehouse.</summary>
    public int     InStock { get; set; }
    /// <summary>Units across every warehouse.</summary>
    public int     Total   { get; set; }
    public decimal AvgCost { get; set; }
    /// <summary>Supplier price on the most recent stock in, to pre-fill the unit cost.</summary>
    public decimal? LastCost { get; set; }
    public bool    TracksSerials { get; set; }
}
