using System;
using System.Collections.Generic;

namespace DymoEnergy.SalesInvoices;

public class CreateUpdateSalesInvoiceDto
{
    public int?    PortalId        { get; set; }
    public string? InvoiceNumber   { get; set; }
    public DateTime  InvoiceDate   { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate       { get; set; }
    public int?    CustomerId      { get; set; }
    public string? CustomerName    { get; set; }
    public string? CustomerEmail   { get; set; }
    public string? CustomerPhone   { get; set; }
    public string? BillingAddress  { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ReferenceNumber { get; set; }
    public string  CurrencyCode    { get; set; } = "BDT";
    public string? Channel         { get; set; }

    // Totals are recalculated server-side from the items. These are still accepted
    // so older clients keep working, but they are ignored.
    public double  Subtotal        { get; set; }
    public double  DiscountTotal   { get; set; }
    public double  TaxTotal        { get; set; }
    public double  GrandTotal      { get; set; }
    public double  BalanceDue      { get; set; }

    public double  ShippingCost       { get; set; }
    public double  AdditionalDiscount { get; set; }
    public string? DiscountNote       { get; set; }
    public bool    TaxInclusive       { get; set; }

    /// <summary>
    /// Create only: records an initial payment of this amount using PaymentMethod.
    /// After creation, payments go through CollectPayment.
    /// </summary>
    public double  AmountPaid      { get; set; }

    /// <summary>Draft, Issued, Cancelled or Refunded. Paid / PartiallyPaid / Overdue are derived.</summary>
    public SalesInvoiceStatus         Status        { get; set; } = SalesInvoiceStatus.Issued;
    public SalesInvoicePaymentMethod? PaymentMethod { get; set; }
    public DateTime?                  PaymentDate   { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public List<CreateUpdateSalesInvoiceItemDto> Items { get; set; } = new();
}
