using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Shipping;

// ── Charges & zones ───────────────────────────────────────────────────────

public class ShippingSettingDto
{
    public bool    FreeDeliveryEnabled    { get; set; }
    [Range(0, 100000000)] public decimal FreeDeliveryOver { get; set; }
    public bool    WeightChargeEnabled    { get; set; }
    [Range(0, 100)] public decimal WeightIncludedKg { get; set; } = 1;
    public bool    CodFeePassed           { get; set; }
    [Range(0, 20)]  public decimal CodFeePercent { get; set; } = 1;
    public bool    CustomerChoosesCourier { get; set; }
    [Range(0, 100000)] public decimal OwnTruckPerKm { get; set; }
    [Range(0, 10000000)] public decimal MinTripCharge { get; set; }
    [Range(1, 60)] public int NoCashAlertDays { get; set; } = 4;
}

public class ShippingZoneDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? Note        { get; set; }
    public decimal Charge      { get; set; }
    public decimal PerExtraKg  { get; set; }
    public decimal CourierCost { get; set; }
    public string? Days        { get; set; }
    public int     Order       { get; set; }
    /// <summary>Charge − courier cost: positive is kept, negative is lost on every parcel.</summary>
    public decimal Margin      { get; set; }
    public int?    PathaoCityId   { get; set; }
    public string? PathaoCityName { get; set; }
    public int?    PathaoZoneId   { get; set; }
    public string? PathaoZoneName { get; set; }
    public decimal CourierPerExtraKg { get; set; }
    public DateTime? PriceCheckedAt  { get; set; }
    public string? PriceError        { get; set; }
}

public class CreateUpdateShippingZoneDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [MaxLength(256)] public string? Note { get; set; }
    [Range(0, 10000000)] public decimal Charge      { get; set; }
    [Range(0, 10000000)] public decimal PerExtraKg  { get; set; }
    [Range(0, 10000000)] public decimal CourierCost { get; set; }
    [MaxLength(64)] public string? Days { get; set; }
    public int Order { get; set; }
    public int? PathaoCityId { get; set; }
    [MaxLength(128)] public string? PathaoCityName { get; set; }
    public int? PathaoZoneId { get; set; }
    [MaxLength(128)] public string? PathaoZoneName { get; set; }
}

/// <summary>Leave <see cref="ZoneId"/> empty to refresh every zone linked to Pathao.</summary>
public class RefreshZonePricesInput
{
    public int? ZoneId { get; set; }
}

public class ZonePriceResultDto
{
    public int     ZoneId  { get; set; }
    public string  Name    { get; set; } = string.Empty;
    public bool    Ok      { get; set; }
    public string  Message { get; set; } = string.Empty;
}

public class RefreshZonePricesResultDto
{
    public List<ZonePriceResultDto> Zones { get; set; } = new();
    /// <summary>Pathao's cash-on-delivery fee %, copied to the Pathao courier settings; null when Pathao did not say.</summary>
    public decimal? CodFeePercent { get; set; }
    /// <summary>What was asked, e.g. "Pathao · live · normal delivery · parcel · 1 kg".</summary>
    public string  Source { get; set; } = string.Empty;
}

public class ShippingItemDto
{
    public int     Id     { get; set; }
    public ShippingItemKind Kind { get; set; }
    public string  Title  { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Extra  { get; set; }
    public string? Color  { get; set; }
    public bool    Flag   { get; set; }
    public int     Order  { get; set; }
}

public class CreateUpdateShippingItemDto
{
    public ShippingItemKind Kind { get; set; }
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Detail { get; set; }
    [MaxLength(128)]  public string? Extra  { get; set; }
    [MaxLength(16)]   public string? Color  { get; set; }
    public bool Flag  { get; set; } = true;
    public int  Order { get; set; }
}

public class ChargesPageDto
{
    public ShippingSettingDto    Setting        { get; set; } = new();
    public List<ShippingZoneDto> Zones          { get; set; } = new();
    public List<ShippingItemDto> BigItems       { get; set; } = new();
    public List<ShippingItemDto> ReturnPolicies { get; set; } = new();
    /// <summary>The Pathao account prices come from; null when none is switched on.</summary>
    public int?   PathaoAccountId { get; set; }
    public string? PathaoAccountName { get; set; }
}

// ── Rules & packaging ─────────────────────────────────────────────────────

public class CourierRuleDto
{
    public int      Id        { get; set; }
    public int      Order     { get; set; }
    public bool     IsEnabled { get; set; }
    public bool     MatchAny  { get; set; }
    public string?  ProductKeyword     { get; set; }
    public decimal? AnyItemOverKg      { get; set; }
    public decimal? TotalWeightUnderKg { get; set; }
    public string?  AddressContains    { get; set; }
    public decimal? CodOver            { get; set; }
    public bool     NeedsInstallation  { get; set; }
    public int?     CourierAccountId   { get; set; }
    public bool     NoParcel  { get; set; }
    public string?  ThenNote  { get; set; }

    public string   IfText    { get; set; } = string.Empty;
    public string   ThenText  { get; set; } = string.Empty;
    public string?  CourierColor { get; set; }
    /// <summary>Orders of the last 90 days this rule was the first match for.</summary>
    public int      MatchCount { get; set; }
}

public class CreateUpdateCourierRuleDto
{
    public int     Order     { get; set; }
    public bool    IsEnabled { get; set; } = true;
    public bool    MatchAny  { get; set; }
    [MaxLength(128)] public string? ProductKeyword  { get; set; }
    [Range(0, 10000)] public decimal? AnyItemOverKg { get; set; }
    [Range(0, 10000)] public decimal? TotalWeightUnderKg { get; set; }
    [MaxLength(128)] public string? AddressContains { get; set; }
    [Range(0, 100000000)] public decimal? CodOver   { get; set; }
    public bool    NeedsInstallation { get; set; }
    public int?    CourierAccountId  { get; set; }
    public bool    NoParcel  { get; set; }
    [MaxLength(256)] public string? ThenNote { get; set; }
}

public class CourierPerformanceDto
{
    public int     CourierAccountId { get; set; }
    public string  Name    { get; set; } = string.Empty;
    public string  Color   { get; set; } = string.Empty;
    public int     Parcels { get; set; }
    /// <summary>Delivered without a return or failure, out of parcels that finished.</summary>
    public int?    SuccessPercent { get; set; }
    /// <summary>Average days from handing over to delivered.</summary>
    public double? AverageDays    { get; set; }
}

public class RulesPageDto
{
    public List<CourierRuleDto>        Rules            { get; set; } = new();
    public List<ShippingItemDto>       PackingRules     { get; set; } = new();
    public List<ShippingItemDto>       CustomerMessages { get; set; } = new();
    public List<CourierPerformanceDto> Performance      { get; set; } = new();
    public List<CourierSummaryDto>     Couriers         { get; set; } = new();
    public int OrdersChecked { get; set; }
    public int Unmatched     { get; set; }
}

// ── Cash on delivery ──────────────────────────────────────────────────────

public class CodCourierDto
{
    public int     CourierAccountId { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  ShortCode { get; set; } = string.Empty;
    public string  Color     { get; set; } = string.Empty;
    public CourierProvider Provider { get; set; }
    public int     Parcels   { get; set; }
    public decimal Collected { get; set; }
    public decimal Fee       { get; set; }
    public decimal ShouldReceive { get; set; }
    public string? Schedule  { get; set; }
    public decimal CodFeePercent { get; set; }
}

public class CodParcelDto
{
    public int      ShipmentId      { get; set; }
    public int      CourierAccountId { get; set; }
    public string?  ConsignmentId   { get; set; }
    public string   OrderNumber     { get; set; } = string.Empty;
    public DateTime DeliveredAt     { get; set; }
    public decimal  Cod             { get; set; }
    public decimal  Fee             { get; set; }
    public decimal  Expected        { get; set; }
}

public class CodPayoutDto
{
    public int      Id          { get; set; }
    public int      CourierAccountId { get; set; }
    public string   CourierName { get; set; } = string.Empty;
    public DateTime Date        { get; set; }
    public decimal  Amount      { get; set; }
    public decimal  Expected    { get; set; }
    public decimal  Difference  { get; set; }
    public int      Parcels     { get; set; }
    /// <summary>matched | short | over</summary>
    public string   Status      { get; set; } = "matched";
    public string?  Reference   { get; set; }
    public string?  Note        { get; set; }
    public bool     Banked      { get; set; }
}

public class CodIssueDto
{
    /// <summary>red | amber | grey</summary>
    public string  Tone   { get; set; } = "amber";
    public string  Title  { get; set; } = string.Empty;
    public string  Text   { get; set; } = string.Empty;
    /// <summary>payout | view-payout</summary>
    public string  Action { get; set; } = "payout";
    public string  ActionLabel { get; set; } = string.Empty;
    public int?    CourierAccountId { get; set; }
    public int?    PayoutId { get; set; }
}

public class CodAccountOptionDto
{
    public int    Id   { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CodPageDto
{
    public decimal Holding          { get; set; }
    public int     HoldingParcels   { get; set; }
    public int?    OldestUnpaidDays { get; set; }
    public decimal ReceivedThisMonth { get; set; }
    public int     ReceivedCount    { get; set; }
    public int     MatchedCount     { get; set; }
    public decimal ShortTotal       { get; set; }
    public int     ShortCount       { get; set; }
    public List<CodCourierDto>       Couriers { get; set; } = new();
    public List<CodPayoutDto>        Payouts  { get; set; } = new();
    public List<CodIssueDto>         Issues   { get; set; } = new();
    public List<CodParcelDto>        Unpaid   { get; set; } = new();
    public List<CodAccountOptionDto> FinanceAccounts { get; set; } = new();
}

public class CreateCourierPayoutDto
{
    public int CourierAccountId { get; set; }
    public DateTime Date { get; set; }
    [Range(0, 100000000)] public decimal Amount { get; set; }
    [MaxLength(128)]  public string? Reference { get; set; }
    [MaxLength(2000)] public string? Note { get; set; }
    [Required, MinLength(1)] public List<int> ShipmentIds { get; set; } = new();
    /// <summary>Also record the money as received in this Financials account.</summary>
    public int? FinanceAccountId { get; set; }
}
