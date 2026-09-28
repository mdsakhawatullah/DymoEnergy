using System.Collections.Generic;

namespace DymoEnergy.Categories;

/// <summary>One category block on the storefront home page: thumbnail + its first products.</summary>
public class HomeCategoryShowcaseDto
{
    public int     Id                { get; set; }
    public string? Name              { get; set; }
    public string? Slug              { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    public string? AccentColor       { get; set; }
    public int     TotalProductCount { get; set; }
    public bool    HasMore           => TotalProductCount > Products.Count;
    public List<HomeShowcaseProductDto> Products { get; set; } = new();
}

public class HomeShowcaseProductDto
{
    public int     Id            { get; set; }
    public string? Name          { get; set; }
    public double  Price         { get; set; }
    public double? DiscountPrice { get; set; }
    public string? PrimaryImage  { get; set; }
}
