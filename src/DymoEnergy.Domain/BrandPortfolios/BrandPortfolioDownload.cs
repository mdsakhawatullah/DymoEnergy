using Volo.Abp.Domain.Entities;

namespace DymoEnergy.BrandPortfolios;

/// <summary>A catalogue, datasheet bundle or price list offered on the brand page.</summary>
public class BrandPortfolioDownload : Entity<int>
{
    public int BrandPortfolioId { get; set; }

    public string? Title { get; set; }
    public BrandDownloadKind Kind { get; set; } = BrandDownloadKind.Pdf;
    public string? FileUrl { get; set; }
    /// <summary>What to print beside the file, e.g. "12 MB" or "12 files".</summary>
    public string? FileSize { get; set; }

    /// <summary>Asks the visitor to sign in before the file is served.</summary>
    public bool DealersOnly { get; set; }

    public bool IsActive { get; set; } = true;
    public int  DisplayOrder { get; set; }
}
