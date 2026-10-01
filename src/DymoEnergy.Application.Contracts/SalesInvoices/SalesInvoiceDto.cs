using System;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.SalesInvoices;

public class SalesInvoiceDto : FullAuditedEntityDto<int>
{
    public int?    PortalId        { get; set; }
    public string? InvoiceNumber   { get; set; }
    public DateTime  InvoiceDate   { get; set; }
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
    public double  Subtotal        { get; set; }
    public double  DiscountTotal   { get; set; }
    public double  AdditionalDiscount { get; set; }
    public string? DiscountNote    { get; set; }
    public bool    TaxInclusive    { get; set; }
    public double  TaxTotal        { get; set; }
    public double  ShippingCost    { get; set; }
    public double  GrandTotal      { get; set; }
    public double  AmountPaid      { get; set; }
    public double  BalanceDue      { get; set; }
    /// <summary>Effective status: Overdue is derived from the due date at read time.</summary>
    public SalesInvoiceStatus         Status        { get; set; }
    public SalesInvoicePaymentMethod? PaymentMethod { get; set; }
    public DateTime?                  PaymentDate   { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }

    // ── List helpers ─────────────────────────────────────────────────────────
    public int     ItemCount     { get; set; }
    public string? FirstItemName { get; set; }
}
