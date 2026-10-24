using System;
using Volo.Abp.Domain.Entities;

namespace DymoEnergy.BrandPortfolios;

/// <summary>A certificate or test report, shown on the shop with a link to the supplier's PDF.</summary>
public class BrandPortfolioCertificate : Entity<int>
{
    public int BrandPortfolioId { get; set; }

    /// <summary>The mark itself — CE, RoHS, IEC 62109, BSTI.</summary>
    public string? Mark { get; set; }
    /// <summary>Which product lines or models it covers.</summary>
    public string? Covers { get; set; }
    /// <summary>The number a customer can check with the issuing body.</summary>
    public string? CertificateNumber { get; set; }

    /// <summary>True for marks that do not lapse, so no date is asked for.</summary>
    public bool NoExpiry { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public string? FileUrl { get; set; }
    /// <summary>What to print beside the file, e.g. "PDF · 1.2 MB".</summary>
    public string? FileSize { get; set; }

    public bool IsVisible { get; set; } = true;
    public int  DisplayOrder { get; set; }
}
