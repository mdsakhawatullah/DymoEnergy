using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Shipping;

/// <summary>Single row: checkout rules and own-truck pricing.</summary>
public class ShippingSetting : AuditedAggregateRoot<int>
{
    public bool    FreeDeliveryEnabled    { get; set; } = true;
    public decimal FreeDeliveryOver       { get; set; } = 30000;
    public bool    WeightChargeEnabled    { get; set; } = true;
    /// <summary>Weight included in the zone charge; each kilo above adds the zone's per-kg amount.</summary>
    public decimal WeightIncludedKg       { get; set; } = 1;
    public bool    CodFeePassed           { get; set; }
    public decimal CodFeePercent          { get; set; } = 1;
    public bool    CustomerChoosesCourier { get; set; }
    public decimal OwnTruckPerKm          { get; set; } = 35;
    public decimal MinTripCharge          { get; set; } = 1200;
    /// <summary>Delivered parcels without a payout after this many days show under "Needs a look".</summary>
    public int     NoCashAlertDays        { get; set; } = 4;
}

/// <summary>A delivery zone with what the customer pays and what the courier costs.</summary>
public class ShippingZone : FullAuditedAggregateRoot<int>
{
    public string  Name        { get; set; } = string.Empty;
    public string? Note        { get; set; }
    public decimal Charge      { get; set; }
    public decimal PerExtraKg  { get; set; }
    public decimal CourierCost { get; set; }
    public string? Days        { get; set; }
    public int     Order       { get; set; }
}

/// <summary>Editable text lists: big items, return policies, packing rules, customer messages.</summary>
public class ShippingListItem : FullAuditedAggregateRoot<int>
{
    public ShippingItemKind Kind { get; set; }
    public string  Title  { get; set; } = string.Empty;
    public string? Detail { get; set; }
    /// <summary>Short label: the method ("Own truck"), the payer ("We pay") or the channel ("SMS").</summary>
    public string? Extra  { get; set; }
    public string? Color  { get; set; }
    public bool    Flag   { get; set; } = true;
    public int     Order  { get; set; }
}

/// <summary>
/// "If … then …" rule choosing the courier for an order. Conditions left empty are ignored;
/// all filled conditions must match. Checked top to bottom, first match wins.
/// </summary>
public class CourierRule : FullAuditedAggregateRoot<int>
{
    public int     Order     { get; set; }
    public bool    IsEnabled { get; set; } = true;
    /// <summary>True: any one filled condition is enough. False: all filled conditions must match.</summary>
    public bool    MatchAny  { get; set; }

    public string? ProductKeyword     { get; set; }
    public decimal? AnyItemOverKg     { get; set; }
    public decimal? TotalWeightUnderKg { get; set; }
    public string? AddressContains    { get; set; }
    public decimal? CodOver           { get; set; }
    public bool    NeedsInstallation  { get; set; }

    /// <summary>Courier to use; null together with <see cref="NoParcel"/> means "no courier parcel".</summary>
    public int?    CourierAccountId { get; set; }
    public bool    NoParcel  { get; set; }
    /// <summary>Extra words shown after "then …", e.g. "book a slot with the installation team".</summary>
    public string? ThenNote  { get; set; }
}

/// <summary>Cash a courier paid out, matched against delivered cash-on-delivery parcels.</summary>
public class CourierPayout : FullAuditedAggregateRoot<int>
{
    public int      CourierAccountId { get; set; }
    public DateTime Date      { get; set; }
    public decimal  Amount    { get; set; }
    /// <summary>What the matched parcels said we should receive, frozen when the payout is saved.</summary>
    public decimal  Expected  { get; set; }
    public string?  Reference { get; set; }
    public string?  Note      { get; set; }
    /// <summary>Money-in row created in Financials, if the payout was banked to an account there.</summary>
    public int?     FinanceTransactionId { get; set; }
}
