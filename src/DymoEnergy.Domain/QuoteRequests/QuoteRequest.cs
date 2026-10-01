using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.QuoteRequests;

/// <summary>A "Get a Quote" enquiry submitted from the storefront quote page.</summary>
public class QuoteRequest : FullAuditedAggregateRoot<int>
{
    public string  Name     { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public string? Email    { get; set; }
    /// <summary>System type, e.g. "Residential rooftop", "Solar irrigation pump".</summary>
    public string? Interest { get; set; }
    public string? Message  { get; set; }
    public QuoteRequestStatus Status { get; set; } = QuoteRequestStatus.New;

    // ── Requirement (optional — sent by the storefront or filled in by staff) ──
    public string? EstimatedSize { get; set; }   // "~5 kW", "7.5 HP"
    public string? MonthlyBill   { get; set; }   // "BDT 6,000–8,000"
    public string? RoofSite      { get; set; }   // "Flat concrete, 1,200 sq ft"
    public string? Location      { get; set; }   // "Halishahar, Chattogram"

    /// <summary>Internal follow-up note, never shown to the customer.</summary>
    public string? AdminNote { get; set; }
}
