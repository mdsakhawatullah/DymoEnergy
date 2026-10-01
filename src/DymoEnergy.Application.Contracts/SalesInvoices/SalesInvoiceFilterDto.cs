using System;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.SalesInvoices;

public class SalesInvoiceFilterDto : PagedAndSortedResultRequestDto
{
    public string?                   Filter       { get; set; }
    public SalesInvoiceStatus?       Status       { get; set; }
    /// <summary>List tab: Paid / Due / Overdue. Null = all.</summary>
    public SalesInvoicePaymentState? PaymentState { get; set; }
    public int?                      CustomerId   { get; set; }
    public int?                      PortalId     { get; set; }
    public DateTime?                 DateFrom     { get; set; }
    public DateTime?                 DateTo       { get; set; }
}
