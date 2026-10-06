using System;
using System.Collections.Generic;

namespace DymoEnergy.Orders;

/// <summary>What a customer types to look their own order up.</summary>
public class TrackOrderInput
{
    public string? OrderNumber { get; set; }
    public string? Phone       { get; set; }
}

/// <summary>The part of an order a customer may see once they have proved it is theirs.</summary>
public class OrderTrackingDto
{
    public string?   OrderNumber           { get; set; }
    public DateTime  OrderDate             { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate    { get; set; }
    public DateTime? LastUpdated           { get; set; }

    public OrderStatus       Status       { get; set; }
    public OrderStage        Stage        { get; set; }
    public OrderShipmentType ShipmentType { get; set; }
    public OrderPaymentType? PaymentType  { get; set; }

    public string? CustomerName    { get; set; }
    public string? DeliveryAddress { get; set; }

    public string CurrencyCode  { get; set; } = "BDT";
    public double Subtotal      { get; set; }
    public double DiscountTotal { get; set; }
    public double ShippingCost  { get; set; }
    public double GrandTotal    { get; set; }
    public double AmountPaid    { get; set; }
    public double BalanceDue    { get; set; }

    public List<OrderTrackingItemDto> Items { get; set; } = new();
}

public class OrderTrackingItemDto
{
    public int?    ProductId   { get; set; }
    public string? ProductName { get; set; }
    public string? Sku         { get; set; }
    public double  Quantity    { get; set; }
    public double  UnitPrice   { get; set; }
    public double  LineTotal   { get; set; }
}
