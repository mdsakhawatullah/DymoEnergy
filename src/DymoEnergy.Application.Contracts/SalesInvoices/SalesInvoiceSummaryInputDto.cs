using System;

namespace DymoEnergy.SalesInvoices;

public class SalesInvoiceSummaryInputDto
{
    public string?   Filter   { get; set; }
    public int?      PortalId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo   { get; set; }
}
