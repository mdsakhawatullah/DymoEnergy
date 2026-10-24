using Volo.Abp.Application.Dtos;

namespace DymoEnergy.BrandPortfolios;

public class GetBrandPortfolioListDto : PagedAndSortedResultRequestDto
{
    /// <summary>Matches the brand name, the web address or the tagline.</summary>
    public string? Filter { get; set; }

    /// <summary>null = both; true = only the pages that are public.</summary>
    public bool? IsPublished { get; set; }
}
