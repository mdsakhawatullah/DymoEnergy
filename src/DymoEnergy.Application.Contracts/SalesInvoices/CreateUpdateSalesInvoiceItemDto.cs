namespace DymoEnergy.SalesInvoices;

public class CreateUpdateSalesInvoiceItemDto
{
    public int?    Id             { get; set; }
    public int?    ProductId      { get; set; }
    public string? ProductName    { get; set; }
    public string? Sku            { get; set; }
    public string? Description    { get; set; }
    public double  Quantity       { get; set; } = 1;
    public double  UnitPrice      { get; set; }
    public double  DiscountPercent { get; set; }
    public double  DiscountAmount  { get; set; }
    public double  TaxRate        { get; set; }
    public double  TaxAmount      { get; set; }
    public double  LineTotal      { get; set; }
    public int     DisplayOrder   { get; set; }
}
