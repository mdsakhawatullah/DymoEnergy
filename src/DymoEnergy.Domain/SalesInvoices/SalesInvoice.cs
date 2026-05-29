using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.SalesInvoices;

public class SalesInvoice : FullAuditedAggregateRoot<int>
{
    public int?    PortalId        { get; set; }
    public string? InvoiceNumber   { get; set; }
    public DateTime  InvoiceDate   { get; set; }
    public DateTime? DueDate       { get; set; }

    // ── Customer ────────────────────────────────────────────────────────────
    public int?    CustomerId      { get; set; }
    public string? CustomerName    { get; set; }
    public string? CustomerEmail   { get; set; }
    public string? CustomerPhone   { get; set; }
    public string? BillingAddress  { get; set; }
    public string? ShippingAddress { get; set; }

    // ── Reference ───────────────────────────────────────────────────────────
    public string? ReferenceNumber { get; set; }
    public string  CurrencyCode    { get; set; } = "BDT";

    // ── Totals ───────────────────────────────────────────────────────────────
    public double Subtotal      { get; set; }
    public double DiscountTotal { get; set; }
    public double TaxTotal      { get; set; }
    public double ShippingCost  { get; set; }
    public double GrandTotal    { get; set; }
    public double AmountPaid    { get; set; }
    public double BalanceDue    { get; set; }

    // ── Status & Payment ─────────────────────────────────────────────────────
    public SalesInvoiceStatus          Status        { get; set; } = SalesInvoiceStatus.Draft;
    public SalesInvoicePaymentMethod?  PaymentMethod { get; set; }
    public DateTime?                   PaymentDate   { get; set; }

    // ── Notes ────────────────────────────────────────────────────────────────
    public string? Notes { get; set; }
    public string? Terms { get; set; }
}
