using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.BrandPortfolios;

public class CreateUpdateBrandPortfolioDto
{
    public int? PortalId { get; set; }

    // ── Basics ────────────────────────────────────────────────────────────
    [Required]
    [StringLength(BrandPortfolioConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    /// <summary>Lowercase letters, numbers and single hyphens — it becomes /brands/{slug}.</summary>
    [Required]
    [StringLength(BrandPortfolioConsts.MaxSlugLength)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "The web address may use lowercase letters, numbers and single hyphens only.")]
    public string Slug { get; set; } = null!;

    [StringLength(BrandPortfolioConsts.MaxTaglineLength)]
    public string? Tagline { get; set; }

    [StringLength(BrandPortfolioConsts.MaxUrlLength)]
    public string? LogoUrl { get; set; }

    [StringLength(BrandPortfolioConsts.MaxIntroLength)]
    public string? ShortIntro { get; set; }

    [StringLength(BrandPortfolioConsts.MaxShortTextLength)]
    public string? DistributorSince { get; set; }

    [StringLength(BrandPortfolioConsts.MaxColorLength)]
    public string? BrandColor { get; set; }

    // ── Story ─────────────────────────────────────────────────────────────
    [StringLength(BrandPortfolioConsts.MaxShortTextLength)]
    public string? StoryHeadline { get; set; }

    // ── Hero ──────────────────────────────────────────────────────────────
    public BrandHeroKind HeroKind { get; set; } = BrandHeroKind.Photo;

    [StringLength(BrandPortfolioConsts.MaxUrlLength)]
    public string? HeroMediaUrl { get; set; }

    [StringLength(BrandPortfolioConsts.MaxShortTextLength)]
    public string? HeroCaption { get; set; }

    public bool HeroAutoplayMuted  { get; set; } = true;
    public bool HeroUsedForSharing { get; set; }

    // ── Call to action ────────────────────────────────────────────────────
    [StringLength(BrandPortfolioConsts.MaxShortTextLength)]
    public string? CtaHeadline { get; set; }

    [StringLength(BrandPortfolioConsts.MaxShortTextLength)]
    public string? CtaSubline { get; set; }

    // ── Search listing ────────────────────────────────────────────────────
    [StringLength(BrandPortfolioConsts.MaxMetaTitleLength)]
    public string? MetaTitle { get; set; }

    [StringLength(BrandPortfolioConsts.MaxMetaDescLength)]
    public string? MetaDescription { get; set; }

    [StringLength(BrandPortfolioConsts.MaxContentJsonLength)]
    public string? ContentJson { get; set; }

    // ── Status ────────────────────────────────────────────────────────────
    public bool IsPublished { get; set; }
    public bool IsActive    { get; set; } = true;
    public int  DisplayOrder { get; set; }

    // ── Child collections ─────────────────────────────────────────────────
    public List<CreateUpdateBrandPortfolioCertificateDto> Certificates { get; set; } = new();
    public List<CreateUpdateBrandPortfolioDownloadDto>    Downloads    { get; set; } = new();
    public List<CreateUpdateBrandPortfolioProductLineDto> ProductLines { get; set; } = new();
    public List<CreateUpdateBrandPortfolioProjectDto>     Projects     { get; set; } = new();
    public List<CreateUpdateBrandPortfolioMediaDto>       Media        { get; set; } = new();
}
