using Volo.Abp.Domain.Entities;

namespace DymoEnergy.BrandPortfolios;

/// <summary>A factory or product photo. The first two sit beside the story; the rest open in the gallery.</summary>
public class BrandPortfolioMedia : Entity<int>
{
    public int BrandPortfolioId { get; set; }

    public string? ImageUrl { get; set; }
    public string? Caption  { get; set; }
    public string? AltText  { get; set; }

    public bool IsActive { get; set; } = true;
    public int  DisplayOrder { get; set; }
}
