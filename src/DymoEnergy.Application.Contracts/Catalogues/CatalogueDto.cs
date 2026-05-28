using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Catalogues;

public class CatalogueDto : FullAuditedEntityDto<int>
{
    public int? PortalId { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public string? HeroTitle    { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroCtaText  { get; set; }
    public string? HeroCtaUrl   { get; set; }
    public string? PrimaryBackgroundImageUrl { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    public string? OverlayColor { get; set; }
    public float   OverlayOpacity { get; set; }
    public string? PrimaryTextColor { get; set; }
    public string? AccentColor { get; set; }
    public string? SectionBackgroundColor { get; set; }
    public CatalogueLayoutType LayoutType   { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }

    // ── SEO ───────────────────────────────────────────────────────────────
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public List<CatalogueImageDto> Images { get; set; } = new();
}
