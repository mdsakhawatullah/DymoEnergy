using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.BrandPortfolios;

/// <summary>
/// One brand page on the storefront (/brands/{slug}) — the maker's own page with its
/// story, figures, certificates, downloads, product lines and photos.
/// </summary>
public class BrandPortfolio : FullAuditedAggregateRoot<int>
{
    public int? PortalId { get; set; }

    // ── Basics ────────────────────────────────────────────────────────────
    public string? Name    { get; set; }
    /// <summary>The last part of the web address: /brands/{Slug}.</summary>
    public string? Slug    { get; set; }
    /// <summary>The italic line beside the name.</summary>
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    /// <summary>Two or three sentences shown under the name.</summary>
    public string? ShortIntro { get; set; }
    /// <summary>Free text so "since 2014" and "2014" both read naturally.</summary>
    public string? DistributorSince { get; set; }
    /// <summary>Hex colour that tints the page.</summary>
    public string? BrandColor { get; set; }

    // ── Story ─────────────────────────────────────────────────────────────
    public string? StoryHeadline { get; set; }

    // ── Hero ──────────────────────────────────────────────────────────────
    public BrandHeroKind HeroKind { get; set; } = BrandHeroKind.Photo;
    public string? HeroMediaUrl { get; set; }
    public string? HeroCaption  { get; set; }
    /// <summary>Video only: start it on its own with the sound off.</summary>
    public bool HeroAutoplayMuted { get; set; } = true;
    /// <summary>Use the hero picture as the preview when the page is shared.</summary>
    public bool HeroUsedForSharing { get; set; }

    // ── Call to action ────────────────────────────────────────────────────
    public string? CtaHeadline { get; set; }
    public string? CtaSubline  { get; set; }

    // ── Search listing ────────────────────────────────────────────────────
    public string? MetaTitle       { get; set; }
    public string? MetaDescription { get; set; }

    /// <summary>
    /// JSON for the repeating narrative blocks: quick stats, "at a glance" facts,
    /// story paragraphs, "why we carry them" reasons, warranty terms and warranty notes.
    /// </summary>
    public string? ContentJson { get; set; }

    // ── Status ────────────────────────────────────────────────────────────
    /// <summary>When false the page is not reachable on the shop.</summary>
    public bool IsPublished { get; set; }
    public bool IsActive    { get; set; } = true;
    public int  DisplayOrder { get; set; }

    // ── Child collections ─────────────────────────────────────────────────
    public ICollection<BrandPortfolioCertificate> Certificates { get; set; } = new List<BrandPortfolioCertificate>();
    public ICollection<BrandPortfolioDownload>    Downloads    { get; set; } = new List<BrandPortfolioDownload>();
    public ICollection<BrandPortfolioProductLine> ProductLines { get; set; } = new List<BrandPortfolioProductLine>();
    public ICollection<BrandPortfolioProject>     Projects     { get; set; } = new List<BrandPortfolioProject>();
    public ICollection<BrandPortfolioMedia>       Media        { get; set; } = new List<BrandPortfolioMedia>();
}
