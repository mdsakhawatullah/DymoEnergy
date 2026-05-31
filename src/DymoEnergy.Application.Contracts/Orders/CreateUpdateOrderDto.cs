using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Orders;

public class CreateUpdateOrderDto
{
    public int?    PortalId { get; set; }
    public DateTime  OrderDate             { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate    { get; set; }

    // ── Customer ──────────────────────────────────────────────────────────
    public int? CustomerId { get; set; }

    [MaxLength(256)]
    public string? CustomerName { get; set; }

    [MaxLength(256)]
    public string? CustomerEmail { get; set; }

    [MaxLength(32)]
    public string? CustomerPhone { get; set; }

    [MaxLength(256)]
    public string? CustomerReference { get; set; }

    // ── Delivery ──────────────────────────────────────────────────────────
    [MaxLength(512)]
    public string? BillingAddress { get; set; }

    [MaxLength(512)]
    public string? DeliveryAddress { get; set; }

    [MaxLength(256)]
    public string? DeliveryContact { get; set; }

    [MaxLength(32)]
    public string? DeliveryPhone { get; set; }

    // ── Classification ────────────────────────────────────────────────────
    public OrderStatus       Status       { get; set; } = OrderStatus.Pending;
    public OrderStage        Stage        { get; set; } = OrderStage.New;
    public OrderPriority     Priority     { get; set; } = OrderPriority.Normal;
    public OrderShipmentType ShipmentType { get; set; } = OrderShipmentType.Standard;
    public OrderPaymentType? PaymentType  { get; set; }
    public OrderCreateMethod CreateMethod { get; set; } = OrderCreateMethod.Web;

    [MaxLength(512)]
    public string? CreatedHow { get; set; }

    // ── Voucher ───────────────────────────────────────────────────────────
    [MaxLength(64)]
    public string? VoucherCode   { get; set; }
    public double  VoucherAmount { get; set; }

    // ── Financials ────────────────────────────────────────────────────────
    [MaxLength(8)]
    public string CurrencyCode  { get; set; } = "BDT";
    public double Subtotal      { get; set; }
    public double DiscountTotal { get; set; }
    public double TaxRate       { get; set; }
    public double TaxTotal      { get; set; }
    public double ShippingCost  { get; set; }
    public double GrandTotal    { get; set; }
    public double AmountPaid    { get; set; }
    public double BalanceDue    { get; set; }

    // ── Payment ───────────────────────────────────────────────────────────
    public DateTime? PaymentDate { get; set; }

    // ── Notes ─────────────────────────────────────────────────────────────
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? NotesInvoice { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    // ── Items ─────────────────────────────────────────────────────────────
    public List<CreateUpdateOrderItemDto> Items { get; set; } = new();
}
