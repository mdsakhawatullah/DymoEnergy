using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Customers;

/// <summary>
/// Someone who buys from us. Orders and invoices keep their own copy of the name and phone as they were
/// at the time, so correcting a customer here never rewrites what was printed on an old invoice.
/// </summary>
public class Customer : FullAuditedAggregateRoot<int>
{
    public string  Name        { get; set; } = string.Empty;
    public string? Phone       { get; set; }
    /// <summary>The phone reduced to 11 digits, for matching orders and for catching duplicates.</summary>
    public string? PhoneKey    { get; set; }
    public string? Email       { get; set; }

    public CustomerType   Type   { get; set; } = CustomerType.Household;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public CustomerSource Source { get; set; } = CustomerSource.Showroom;

    /// <summary>Only for a business, dealer or institution.</summary>
    public string? CompanyName { get; set; }
    public string? TaxId       { get; set; }

    public string? Address  { get; set; }
    public string? Area     { get; set; }
    public string? City     { get; set; }
    public string? District { get; set; }

    /// <summary>Who looks after this customer.</summary>
    public string? AssignedTo { get; set; }
    public string? Note       { get; set; }
    /// <summary>Comma separated labels, e.g. "solar pump, repeat".</summary>
    public string? Tags       { get; set; }

    /// <summary>How much they may owe at once. Zero means cash only.</summary>
    public decimal CreditLimit    { get; set; }
    /// <summary>Days to pay an invoice. Zero means on delivery.</summary>
    public int     PaymentTermDays { get; set; }

    /// <summary>When we first heard from them; may be earlier than when this record was made.</summary>
    public DateTime? FirstSeen { get; set; }
}
