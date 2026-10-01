using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.QuoteRequests;

/// <summary>Staff edits from the admin detail panel (after a call / site visit).</summary>
public class UpdateQuoteRequestDetailsDto
{
    [StringLength(QuoteRequestConsts.MaxInterestLength)]
    public string? Interest { get; set; }

    [StringLength(QuoteRequestConsts.MaxDetailLength)]
    public string? EstimatedSize { get; set; }

    [StringLength(QuoteRequestConsts.MaxDetailLength)]
    public string? MonthlyBill { get; set; }

    [StringLength(QuoteRequestConsts.MaxDetailLength)]
    public string? RoofSite { get; set; }

    [StringLength(QuoteRequestConsts.MaxDetailLength)]
    public string? Location { get; set; }

    [StringLength(QuoteRequestConsts.MaxAdminNoteLength)]
    public string? AdminNote { get; set; }
}
