using System;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.SalesInvoices;

public class CollectSalesInvoicePaymentDto
{
    [Range(0.01, double.MaxValue)]
    public double                    Amount          { get; set; }
    public SalesInvoicePaymentMethod Method          { get; set; } = SalesInvoicePaymentMethod.Cash;
    public DateTime?                 PaidOn          { get; set; }
    [StringLength(128)]
    public string?                   ReferenceNumber { get; set; }
    [StringLength(512)]
    public string?                   Note            { get; set; }
}
