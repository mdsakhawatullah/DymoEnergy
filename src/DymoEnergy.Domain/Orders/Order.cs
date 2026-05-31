using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Orders;

public class Order : FullAuditedAggregateRoot<int>
{
    public int?    PortalId    { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime  OrderDate          { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate   { get; set; }

    // ── Customer ──────────────────────────────────────────────────────────
    public int?    CustomerId    { get; set; }
    public string? CustomerName  { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    /// <summary>Customer's own reference/PO number.</summary>
    public string? CustomerReference { get; set; }

    // ── Delivery ──────────────────────────────────────────────────────────
    public string? BillingAddress  { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryContact { get; set; }
    public string? DeliveryPhone   { get; set; }

    // ── Classification ────────────────────────────────────────────────────
    public OrderStatus       Status       { get; set; } = OrderStatus.Pending;
    public OrderStage        Stage        { get; set; } = OrderStage.New;
    public OrderPriority     Priority     { get; set; } = OrderPriority.Normal;
    public OrderShipmentType ShipmentType { get; set; } = OrderShipmentType.Standard;
    public OrderPaymentType? PaymentType  { get; set; }
    public OrderCreateMethod CreateMethod { get; set; } = OrderCreateMethod.Web;

    /// <summary>Free-text description of how / where this order originated.</summary>
    public string? CreatedHow { get; set; }

    // ── Voucher ───────────────────────────────────────────────────────────
    public string? VoucherCode   { get; set; }
    public double  VoucherAmount { get; set; }

    // ── Financials ────────────────────────────────────────────────────────
    public string CurrencyCode   { get; set; } = "BDT";
    public double Subtotal       { get; set; }
    public double DiscountTotal  { get; set; }
    public double TaxRate        { get; set; }
    public double TaxTotal       { get; set; }
    public double ShippingCost   { get; set; }
    public double GrandTotal     { get; set; }
    public double AmountPaid     { get; set; }
    public double BalanceDue     { get; set; }

    // ── Payment ───────────────────────────────────────────────────────────
    public DateTime? PaymentDate { get; set; }

    // ── Notes ─────────────────────────────────────────────────────────────
    public string? Notes        { get; set; }
    public string? NotesInvoice { get; set; }
    public string? Terms        { get; set; }
    public string? InternalNotes { get; set; }
}
