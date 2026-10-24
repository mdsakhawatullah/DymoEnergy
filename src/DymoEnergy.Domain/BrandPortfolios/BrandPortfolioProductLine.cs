using Volo.Abp.Domain.Entities;

namespace DymoEnergy.BrandPortfolios;

/// <summary>A product line listed on the brand page, optionally tied to a catalogue category.</summary>
public class BrandPortfolioProductLine : Entity<int>
{
    public int BrandPortfolioId { get; set; }

    public string? Name  { get; set; }
    /// <summary>The one-line blurb under the line's name.</summary>
    public string? Blurb { get; set; }

    /// <summary>Set it and the counts below are refreshed from the catalogue.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Which icon to draw beside the line.</summary>
    public string? IconKey { get; set; }

    public bool IsVisible { get; set; } = true;
    public int  DisplayOrder { get; set; }
}
