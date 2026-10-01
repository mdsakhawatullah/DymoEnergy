using System;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.SalesInvoices;

public class SalesInvoicePaymentDto : CreationAuditedEntityDto<int>
{
    public int                       InvoiceId       { get; set; }
    public double                    Amount          { get; set; }
    public SalesInvoicePaymentMethod Method          { get; set; }
    public DateTime                  PaidOn          { get; set; }
    public string?                   ReferenceNumber { get; set; }
    public string?                   Note            { get; set; }
}
