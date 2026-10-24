using System;

namespace DymoEnergy.BrandPortfolios;

/// <summary>Id is null for a row the editor has just added.</summary>
public class CreateUpdateBrandPortfolioCertificateDto
{
    public int?      Id { get; set; }
    public string?   Mark { get; set; }
    public string?   Covers { get; set; }
    public string?   CertificateNumber { get; set; }
    public bool      NoExpiry { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string?   FileUrl { get; set; }
    public string?   FileSize { get; set; }
    public bool      IsVisible { get; set; } = true;
    public int       DisplayOrder { get; set; }
}

public class CreateUpdateBrandPortfolioDownloadDto
{
    public int?   Id { get; set; }
    public string? Title { get; set; }
    public BrandDownloadKind Kind { get; set; } = BrandDownloadKind.Pdf;
    public string? FileUrl { get; set; }
    public string? FileSize { get; set; }
    public bool   DealersOnly { get; set; }
    public bool   IsActive { get; set; } = true;
    public int    DisplayOrder { get; set; }
}

public class CreateUpdateBrandPortfolioProductLineDto
{
    public int?   Id { get; set; }
    public string? Name { get; set; }
    public string? Blurb { get; set; }
    public int?   CategoryId { get; set; }
    public string? IconKey { get; set; }
    public bool   IsVisible { get; set; } = true;
    public int    DisplayOrder { get; set; }
}

public class CreateUpdateBrandPortfolioProjectDto
{
    public int?   Id { get; set; }
    public int?   ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public BrandProjectConsentStatus ConsentStatus { get; set; } = BrandProjectConsentStatus.WaitingOnConsent;
    public int    DisplayOrder { get; set; }
}

public class CreateUpdateBrandPortfolioMediaDto
{
    public int?   Id { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public bool   IsActive { get; set; } = true;
    public int    DisplayOrder { get; set; }
}
