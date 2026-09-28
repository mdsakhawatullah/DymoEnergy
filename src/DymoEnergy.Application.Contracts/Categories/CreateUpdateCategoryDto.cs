using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Categories;

public class CreateUpdateCategoryDto
{
    public int? PortalId { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroCtaText { get; set; }
    public string? HeroCtaUrl { get; set; }
    public string? PrimaryBackgroundImageUrl { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    public string? OverlayColor { get; set; }

    [Range(0.0, 1.0)]
    public float OverlayOpacity { get; set; } = 0.4f;
    public string? PrimaryTextColor { get; set; }
    public string? AccentColor { get; set; }
    public string? SectionBackgroundColor { get; set; }

    // ── Layout / Status ───────────────────────────────────────────────────
    public CategoryLayoutType LayoutType   { get; set; } = CategoryLayoutType.FullWidthHero;
    public bool IsPublished  { get; set; }
    public bool IsFeatured   { get; set; }
    public int DisplayOrder { get; set; }

    // ── SEO ───────────────────────────────────────────────────────────────
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public List<CreateUpdateCategoryImageDto> Images { get; set; } = new();
}
