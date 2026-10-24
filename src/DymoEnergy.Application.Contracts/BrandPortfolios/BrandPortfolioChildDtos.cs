using System;

namespace DymoEnergy.BrandPortfolios;

public class BrandPortfolioCertificateDto
{
    public int       Id { get; set; }
    public int       BrandPortfolioId { get; set; }
    public string?   Mark { get; set; }
    public string?   Covers { get; set; }
    public string?   CertificateNumber { get; set; }
    public bool      NoExpiry { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string?   FileUrl { get; set; }
    public string?   FileSize { get; set; }
    public bool      IsVisible { get; set; }
    public int       DisplayOrder { get; set; }
}

public class BrandPortfolioDownloadDto
{
    public int    Id { get; set; }
    public int    BrandPortfolioId { get; set; }
    public string? Title { get; set; }
    public BrandDownloadKind Kind { get; set; }
    public string? FileUrl { get; set; }
    public string? FileSize { get; set; }
    public bool   DealersOnly { get; set; }
    public bool   IsActive { get; set; }
    public int    DisplayOrder { get; set; }
}

public class BrandPortfolioProductLineDto
{
    public int    Id { get; set; }
    public int    BrandPortfolioId { get; set; }
    public string? Name { get; set; }
    public string? Blurb { get; set; }
    public int?   CategoryId { get; set; }
    public string? IconKey { get; set; }
    public bool   IsVisible { get; set; }
    public int    DisplayOrder { get; set; }

    /// <summary>How many live products sit in the linked category. Worked out when the page is read.</summary>
    public int     ProductCount { get; set; }
    /// <summary>The cheapest live product in the linked category, or null when there is none.</summary>
    public double? FromPrice { get; set; }
}

public class BrandPortfolioProjectDto
{
    public int    Id { get; set; }
    public int    BrandPortfolioId { get; set; }
    public int?   ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public BrandProjectConsentStatus ConsentStatus { get; set; }
    public int    DisplayOrder { get; set; }
}

public class BrandPortfolioMediaDto
{
    public int    Id { get; set; }
    public int    BrandPortfolioId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public bool   IsActive { get; set; }
    public int    DisplayOrder { get; set; }
}
