using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.SalesInvoices;

public class SalesInvoiceItem : FullAuditedAggregateRoot<int>
{
    // ── FK (no navigation) ──────────────────────────────────────────────────
    public int InvoiceId { get; set; }

    // ── Product (denormalized) ───────────────────────────────────────────────
    public int?    ProductId   { get; set; }
    public string? ProductName { get; set; }
    public string? Sku         { get; set; }
    public string? Description { get; set; }

    // ── Pricing ──────────────────────────────────────────────────────────────
    public double Quantity       { get; set; }
    public double UnitPrice      { get; set; }
    public double DiscountPercent { get; set; }
    public double DiscountAmount  { get; set; }
    public double TaxRate        { get; set; }
    public double TaxAmount      { get; set; }
    public double LineTotal      { get; set; }

    public int DisplayOrder { get; set; }
}
