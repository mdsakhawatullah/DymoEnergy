using Volo.Abp.Domain.Entities;

namespace DymoEnergy.BrandPortfolios;

/// <summary>A finished job shown on the brand page. Picked from Project planning.</summary>
public class BrandPortfolioProject : Entity<int>
{
    public int BrandPortfolioId { get; set; }

    /// <summary>The job in Project planning, when it was picked from there.</summary>
    public int? ProjectId { get; set; }

    public string? Title    { get; set; }
    /// <summary>The short line under the title, e.g. "5 kW hybrid · 12 panels".</summary>
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }

    public BrandProjectConsentStatus ConsentStatus { get; set; } = BrandProjectConsentStatus.WaitingOnConsent;

    public int DisplayOrder { get; set; }
}
