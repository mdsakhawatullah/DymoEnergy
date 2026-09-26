using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.QuoteRequests;

public class CreateQuoteRequestDto
{
    [Required]
    [StringLength(QuoteRequestConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(QuoteRequestConsts.MaxPhoneLength)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(QuoteRequestConsts.MaxEmailLength)]
    public string? Email { get; set; }

    [StringLength(QuoteRequestConsts.MaxInterestLength)]
    public string? Interest { get; set; }

    [StringLength(QuoteRequestConsts.MaxMessageLength)]
    public string? Message { get; set; }
}
