using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.QuoteRequests;

/// <summary>A "Get a Quote" enquiry submitted from the storefront quote page.</summary>
public class QuoteRequest : FullAuditedAggregateRoot<int>
{
    public string  Name     { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public string? Email    { get; set; }
    public string? Interest { get; set; }
    public string? Message  { get; set; }
    public QuoteRequestStatus Status { get; set; } = QuoteRequestStatus.New;
}
