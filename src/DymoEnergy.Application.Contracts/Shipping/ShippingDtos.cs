using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Shipping;

// ── Couriers ──────────────────────────────────────────────────────────────

public class CourierFieldDto
{
    public string Key      { get; set; } = string.Empty;
    public string Label    { get; set; } = string.Empty;
    public string? Help    { get; set; }
    public bool   IsSecret { get; set; }
    public bool   HasValue { get; set; }
    /// <summary>Plain value for non-secret fields; "•••• last4" for secrets.</summary>
    public string? Display { get; set; }
    public DateTime? ChangedAt { get; set; }
}

public class CourierEnvironmentDto
{
    public CourierEnvironment Environment { get; set; }
    public bool KeysSaved { get; set; }
    public List<CourierFieldDto> Fields { get; set; } = new();
}

public class CourierSummaryDto
{
    public int     Id          { get; set; }
    public CourierProvider Provider { get; set; }
    public string  DisplayName { get; set; } = string.Empty;
    public string  ShortCode   { get; set; } = string.Empty;
    public string  Color       { get; set; } = string.Empty;
    public bool    IsEnabled   { get; set; }
    public int     Order       { get; set; }
    public CourierEnvironment ActiveEnvironment { get; set; }
    /// <summary>True when the app can call this courier's API (only Pathao today).</summary>
    public bool    ApiAvailable { get; set; }
    /// <summary>True for couriers without an API: own delivery team and customer pickup.</summary>
    public bool    IsManual    { get; set; }
    /// <summary>connected | keys | not-connected | always | off</summary>
    public string  Status      { get; set; } = "not-connected";
    public string  StatusText  { get; set; } = string.Empty;
}

public class CourierLogDto
{
    public DateTime Time       { get; set; }
    public CourierEnvironment Environment { get; set; }
    public string   Action     { get; set; } = string.Empty;
    public string?  Method     { get; set; }
    public string?  Endpoint   { get; set; }
    public int?     StatusCode { get; set; }
    public int?     DurationMs { get; set; }
    public string?  Result     { get; set; }
    public bool     IsError    { get; set; }
}

public class CourierDetailDto : CourierSummaryDto
{
    public List<CourierEnvironmentDto> Environments { get; set; } = new();
    public string?   PickupStoreId     { get; set; }
    public string?   PickupStoreName   { get; set; }
    public int       DefaultDeliveryType { get; set; }
    public int       DefaultItemType   { get; set; }
    public decimal   DefaultWeightKg   { get; set; }
    public decimal   CodFeePercent     { get; set; }
    public string?   PayoutSchedule    { get; set; }
    public DateTime? TokenIssuedAt     { get; set; }
    public DateTime? TokenExpiresAt    { get; set; }
    /// <summary>Path to give the courier for status updates, relative to the API host.</summary>
    public string?   WebhookPath       { get; set; }
    public DateTime? LastWebhookAt     { get; set; }
    public string?   LastWebhookNote   { get; set; }
    public DateTime? LastParcelAt      { get; set; }
    public List<CourierLogDto> Logs { get; set; } = new();
}

public class CreateCourierDto
{
    public CourierProvider Provider { get; set; }
    [Required, MaxLength(256)] public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(8)]   public string ShortCode   { get; set; } = string.Empty;
    [Required, MaxLength(16)]  public string Color       { get; set; } = "#6B7280";
}

public class UpdateCourierSettingsDto
{
    [Required, MaxLength(256)] public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(8)]   public string ShortCode   { get; set; } = string.Empty;
    [Required, MaxLength(16)]  public string Color       { get; set; } = "#6B7280";
    public bool IsEnabled { get; set; } = true;
    [MaxLength(128)] public string? PickupStoreId   { get; set; }
    [MaxLength(256)] public string? PickupStoreName { get; set; }
    public int DefaultDeliveryType { get; set; } = 48;
    public int DefaultItemType     { get; set; } = 2;
    [Range(0.5, 10)] public decimal DefaultWeightKg { get; set; } = 1;
    [Range(0, 20)]   public decimal CodFeePercent   { get; set; }
    [MaxLength(128)] public string? PayoutSchedule  { get; set; }
}

public class UpdateCourierCredentialDto
{
    public CourierEnvironment Environment { get; set; }
    [Required, MaxLength(64)]   public string Key   { get; set; } = string.Empty;
    /// <summary>Empty clears the key.</summary>
    [MaxLength(2000)] public string? Value { get; set; }
}

public class CourierCredentialKeyDto
{
    public CourierEnvironment Environment { get; set; }
    [Required, MaxLength(64)] public string Key { get; set; } = string.Empty;
}

public class SetCourierEnvironmentDto
{
    public CourierEnvironment Environment { get; set; }
}

public class CourierTestResultDto
{
    public bool    Ok      { get; set; }
    public string  Message { get; set; } = string.Empty;
    public DateTime? TokenExpiresAt { get; set; }
}

// ── Pathao lookups ────────────────────────────────────────────────────────

public class PathaoStoreDto
{
    public string StoreId   { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string? Address  { get; set; }
    public bool   IsActive  { get; set; }
    public bool   IsDefault { get; set; }
}

public class CreatePathaoStoreDto
{
    [Required, StringLength(50, MinimumLength = 3)]  public string Name          { get; set; } = string.Empty;
    [Required, StringLength(50, MinimumLength = 3)]  public string ContactName   { get; set; } = string.Empty;
    [Required, StringLength(11, MinimumLength = 11)] public string ContactNumber { get; set; } = string.Empty;
    [StringLength(11, MinimumLength = 11)] public string? SecondaryContact { get; set; }
    [StringLength(11, MinimumLength = 11)] public string? OtpNumber        { get; set; }
    [Required, StringLength(120, MinimumLength = 15)] public string Address { get; set; } = string.Empty;
    public int CityId { get; set; }
    public int ZoneId { get; set; }
    public int AreaId { get; set; }
}

public class PathaoLocationDto
{
    public int    Id   { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool?  HomeDeliveryAvailable { get; set; }
    public bool?  PickupAvailable       { get; set; }
}

public class PathaoPriceInputDto
{
    public int     ItemType     { get; set; } = 2;
    public int     DeliveryType { get; set; } = 48;
    [Range(0.5, 10)] public decimal WeightKg { get; set; } = 0.5m;
    public int     CityId       { get; set; }
    public int     ZoneId       { get; set; }
}

public class PathaoPriceDto
{
    public decimal Price            { get; set; }
    public decimal Discount         { get; set; }
    public decimal PromoDiscount    { get; set; }
    public decimal AdditionalCharge { get; set; }
    public decimal CodPercentage    { get; set; }
    public decimal FinalPrice       { get; set; }
}

// ── Shipments ─────────────────────────────────────────────────────────────

public class ShippingOverviewDto
{
    public int     ConnectedCount   { get; set; }
    public int     TotalCount       { get; set; }
    public string  ConnectedNames   { get; set; } = string.Empty;
    public bool    AnyLiveKeys      { get; set; }
    public int     ParcelsMoving    { get; set; }
    public int     ReadyToSend      { get; set; }
    /// <summary>Cash collected on delivered parcels (payout matching comes later).</summary>
    public decimal CodDelivered     { get; set; }
    public int     FailedCallsToday { get; set; }
    public string? LastFailedCall   { get; set; }
    public List<CourierSummaryDto> Couriers { get; set; } = new();
}

public class ReadyOrderDto
{
    public int     OrderId      { get; set; }
    public string  OrderNumber  { get; set; } = string.Empty;
    public string  CustomerName { get; set; } = string.Empty;
    public string? Phone        { get; set; }
    public string? Address      { get; set; }
    public string  Items        { get; set; } = string.Empty;
    public decimal WeightKg     { get; set; }
    /// <summary>True when no product had a weight and the courier default was used.</summary>
    public bool    WeightGuessed { get; set; }
    public decimal Collect      { get; set; }
    public int?    SuggestedCourierId   { get; set; }
    public string? SuggestedCourierName { get; set; }
    /// <summary>Things that would make the courier reject the parcel.</summary>
    public List<string> Problems { get; set; } = new();
    /// <summary>Which courier rule (1-based, enabled rules only) chose the suggestion; null when none matched.</summary>
    public int?    RuleNumber   { get; set; }
    /// <summary>The matching rule says this order gets no courier parcel (for example installation jobs).</summary>
    public bool    NoParcel     { get; set; }
}

public class SendParcelsDto
{
    [Required, MinLength(1)] public List<int> OrderIds { get; set; } = new();
    public int  CourierAccountId { get; set; }
    public int? DeliveryType { get; set; }
    public int? ItemType     { get; set; }
    [MaxLength(256)] public string? SpecialInstruction { get; set; }
}

public class SendParcelResultDto
{
    public int     OrderId       { get; set; }
    public string  OrderNumber   { get; set; } = string.Empty;
    public bool    Ok            { get; set; }
    public string? ConsignmentId { get; set; }
    public decimal? DeliveryFee  { get; set; }
    public string  Message       { get; set; } = string.Empty;
}

public class ShipmentDto
{
    public int      Id             { get; set; }
    public int      OrderId        { get; set; }
    public string   OrderNumber    { get; set; } = string.Empty;
    public string?  ConsignmentId  { get; set; }
    public int      CourierAccountId { get; set; }
    public string   CourierName    { get; set; } = string.Empty;
    public string   CourierColor   { get; set; } = string.Empty;
    public CourierProvider CourierProvider { get; set; }
    public CourierEnvironment Environment { get; set; }
    public string   Status         { get; set; } = string.Empty;
    /// <summary>ready | picked | transit | delivered | failed | returned | cancelled</summary>
    public string   Stage          { get; set; } = string.Empty;
    public DateTime? StatusAt      { get; set; }
    public DateTime CreationTime   { get; set; }
    public string   RecipientName  { get; set; } = string.Empty;
    public string   RecipientAddress { get; set; } = string.Empty;
    public decimal  CodAmount      { get; set; }
    public decimal  DeliveryFee    { get; set; }
    public int      DeliveryType   { get; set; }
}

public class ShipmentEventDto
{
    public DateTime Time   { get; set; }
    public string   Status { get; set; } = string.Empty;
    public string   Stage  { get; set; } = string.Empty;
    /// <summary>sent | webhook | tracked | now</summary>
    public string   Source { get; set; } = string.Empty;
    public string?  Event  { get; set; }
    public string?  Note   { get; set; }
    public decimal? CollectedAmount { get; set; }
}

public class ShipmentOrderItemDto
{
    public string  Name      { get; set; } = string.Empty;
    public string? Sku       { get; set; }
    public double  Quantity  { get; set; }
    public double  UnitPrice { get; set; }
    public double  LineTotal { get; set; }
}

public class ShipmentOrderDto
{
    public int       Id            { get; set; }
    public string    Number        { get; set; } = string.Empty;
    public DateTime  Date          { get; set; }
    public string    Status        { get; set; } = string.Empty;
    public string?   PaymentType   { get; set; }
    public string?   CustomerName  { get; set; }
    public string?   CustomerPhone { get; set; }
    public string?   CustomerEmail { get; set; }
    public string?   DeliveryContact { get; set; }
    public string?   DeliveryPhone   { get; set; }
    public string?   DeliveryAddress { get; set; }
    public string?   Notes         { get; set; }
    public double    Subtotal      { get; set; }
    public double    DiscountTotal { get; set; }
    public double    TaxTotal      { get; set; }
    public double    ShippingCost  { get; set; }
    public double    GrandTotal    { get; set; }
    public double    AmountPaid    { get; set; }
    public double    BalanceDue    { get; set; }
    public List<ShipmentOrderItemDto> Items { get; set; } = new();
}

/// <summary>Everything about one parcel: the parcel, its status history and the order it carries.</summary>
public class ShipmentDetailDto
{
    public ShipmentDto Shipment   { get; set; } = new();
    public string?  MerchantOrderId { get; set; }
    public string   RecipientPhone  { get; set; } = string.Empty;
    public decimal  WeightKg        { get; set; }
    public int      ItemType        { get; set; }
    public string?  Note            { get; set; }
    public string   CourierShortCode { get; set; } = string.Empty;
    public bool     CanTrack        { get; set; }
    public string?  PayoutNote      { get; set; }
    public List<ShipmentEventDto> Events { get; set; } = new();
    public ShipmentOrderDto? Order  { get; set; }
}

public class ShipmentCountsDto
{
    public int Ready     { get; set; }
    public int PickedUp  { get; set; }
    public int InTransit { get; set; }
    public int DeliveredToday { get; set; }
    public int FailedOrReturning { get; set; }
}

public class ShipmentsPageDto
{
    public List<ReadyOrderDto> Ready     { get; set; } = new();
    public ShipmentCountsDto   Counts    { get; set; } = new();
    public List<ShipmentDto>   Shipments { get; set; } = new();
}

public class GetShipmentsInput
{
    [MaxLength(128)] public string? Filter { get; set; }
}
