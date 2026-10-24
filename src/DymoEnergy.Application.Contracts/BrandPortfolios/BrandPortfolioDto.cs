using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.BrandPortfolios;

public class BrandPortfolioDto : FullAuditedEntityDto<int>
{
    public int? PortalId { get; set; }

    // ── Basics ────────────────────────────────────────────────────────────
    public string? Name    { get; set; }
    public string? Slug    { get; set; }
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    public string? ShortIntro { get; set; }
    public string? DistributorSince { get; set; }
    public string? BrandColor { get; set; }

    // ── Story ─────────────────────────────────────────────────────────────
    public string? StoryHeadline { get; set; }

    // ── Hero ──────────────────────────────────────────────────────────────
    public BrandHeroKind HeroKind { get; set; }
    public string? HeroMediaUrl { get; set; }
    public string? HeroCaption  { get; set; }
    public bool HeroAutoplayMuted  { get; set; }
    public bool HeroUsedForSharing { get; set; }

    // ── Call to action ────────────────────────────────────────────────────
    public string? CtaHeadline { get; set; }
    public string? CtaSubline  { get; set; }

    // ── Search listing ────────────────────────────────────────────────────
    public string? MetaTitle       { get; set; }
    public string? MetaDescription { get; set; }

    /// <summary>Quick stats, facts, story paragraphs, reasons and warranty rows as one JSON document.</summary>
    public string? ContentJson { get; set; }

    // ── Status ────────────────────────────────────────────────────────────
    public bool IsPublished { get; set; }
    public bool IsActive    { get; set; }
    public int  DisplayOrder { get; set; }

    // ── Child collections ─────────────────────────────────────────────────
    public List<BrandPortfolioCertificateDto> Certificates { get; set; } = new();
    public List<BrandPortfolioDownloadDto>    Downloads    { get; set; } = new();
    public List<BrandPortfolioProductLineDto> ProductLines { get; set; } = new();
    public List<BrandPortfolioProjectDto>     Projects     { get; set; } = new();
    public List<BrandPortfolioMediaDto>       Media        { get; set; } = new();
}

/// <summary>Row shape for the brands list — no child collections, so the list stays one query.</summary>
public class BrandPortfolioListDto : FullAuditedEntityDto<int>
{
    public string? Name    { get; set; }
    public string? Slug    { get; set; }
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    public string? BrandColor { get; set; }
    public bool    IsPublished { get; set; }
    public bool    IsActive { get; set; }
    public int     DisplayOrder { get; set; }

    public int CertificateCount { get; set; }
    public int ProductLineCount { get; set; }

    /// <summary>How much of the page is filled in, 0–100. Drives the bar in the admin list.</summary>
    public int CompletenessPercent { get; set; }
}
