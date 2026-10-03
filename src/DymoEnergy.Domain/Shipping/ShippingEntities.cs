using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Shipping;

/// <summary>One courier the business can hand parcels to (Pathao, own truck, customer pickup…).</summary>
public class CourierAccount : FullAuditedAggregateRoot<int>
{
    public CourierProvider Provider { get; set; }
    public string  DisplayName { get; set; } = string.Empty;
    public string  ShortCode   { get; set; } = string.Empty;
    public string  Color       { get; set; } = "#6B7280";
    public bool    IsEnabled   { get; set; } = true;
    public int     Order       { get; set; }
    /// <summary>Which set of keys is used for real calls.</summary>
    public CourierEnvironment ActiveEnvironment { get; set; } = CourierEnvironment.Sandbox;

    // ── Parcel defaults (Pathao) ──────────────────────────────────────────
    public string?  PickupStoreId     { get; set; }
    public string?  PickupStoreName   { get; set; }
    /// <summary>48 normal, 12 on demand.</summary>
    public int      DefaultDeliveryType { get; set; } = 48;
    /// <summary>1 document, 2 parcel.</summary>
    public int      DefaultItemType   { get; set; } = 2;
    public decimal  DefaultWeightKg   { get; set; } = 1;

    // ── Webhook ───────────────────────────────────────────────────────────
    public DateTime? LastWebhookAt   { get; set; }
    public string?   LastWebhookNote { get; set; }

    // ── Cash on delivery ──────────────────────────────────────────────────
    /// <summary>The courier’s cut of the cash it collects (Pathao: about 1%).</summary>
    public decimal   CodFeePercent   { get; set; }
    /// <summary>Free text, e.g. "Sun & Wed".</summary>
    public string?   PayoutSchedule  { get; set; }
}

/// <summary>
/// One key of one courier in one environment (base URL, client id, secret, token…).
/// The value is always stored encrypted; secrets are never sent to the browser unless revealed.
/// </summary>
public class CourierCredential : AuditedAggregateRoot<int>
{
    public int CourierAccountId { get; set; }
    public CourierEnvironment Environment { get; set; }
    public string Key { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
}

/// <summary>One call to a courier API (or a key change). Never stores request bodies or secrets.</summary>
public class CourierApiLog : AggregateRoot<int>
{
    public int      CourierAccountId { get; set; }
    public CourierEnvironment Environment { get; set; }
    public DateTime Time       { get; set; }
    public string   Action     { get; set; } = string.Empty;
    public string?  Method     { get; set; }
    public string?  Endpoint   { get; set; }
    public int?     StatusCode { get; set; }
    public int?     DurationMs { get; set; }
    public string?  Result     { get; set; }
    public bool     IsError    { get; set; }
    public Guid?    UserId     { get; set; }
}

/// <summary>A parcel handed to a courier for one order.</summary>
public class Shipment : FullAuditedAggregateRoot<int>
{
    public int      OrderId          { get; set; }
    public int      CourierAccountId { get; set; }
    public CourierEnvironment Environment { get; set; }
    public string?  ConsignmentId    { get; set; }
    public string?  MerchantOrderId  { get; set; }
    public string   Status           { get; set; } = "Pending";
    public string?  StatusSlug       { get; set; }
    public DateTime? StatusAt        { get; set; }
    public decimal  DeliveryFee      { get; set; }
    public decimal  CodAmount        { get; set; }
    public decimal  WeightKg         { get; set; }
    public int      DeliveryType     { get; set; }
    public int      ItemType         { get; set; }
    public string   RecipientName    { get; set; } = string.Empty;
    public string   RecipientPhone   { get; set; } = string.Empty;
    public string   RecipientAddress { get; set; } = string.Empty;
    public string?  Note             { get; set; }
    /// <summary>Set once the courier has paid this parcel’s cash out.</summary>
    public int?     PayoutId         { get; set; }
}
