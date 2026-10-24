using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.BrandPortfolios;

[Authorize(DymoEnergyPermissions.BrandPortfolios.Default)]
public class BrandPortfolioAppService : ApplicationService, IBrandPortfolioAppService
{
    private readonly IRepository<BrandPortfolio, int>            _repository;
    private readonly IRepository<BrandPortfolioCertificate, int> _certificateRepository;
    private readonly IRepository<BrandPortfolioDownload, int>    _downloadRepository;
    private readonly IRepository<BrandPortfolioProductLine, int> _productLineRepository;
    private readonly IRepository<BrandPortfolioProject, int>     _projectRepository;
    private readonly IRepository<BrandPortfolioMedia, int>       _mediaRepository;
    private readonly IRepository<Product, int>                   _productRepository;

    public BrandPortfolioAppService(
        IRepository<BrandPortfolio, int>            repository,
        IRepository<BrandPortfolioCertificate, int> certificateRepository,
        IRepository<BrandPortfolioDownload, int>    downloadRepository,
        IRepository<BrandPortfolioProductLine, int> productLineRepository,
        IRepository<BrandPortfolioProject, int>     projectRepository,
        IRepository<BrandPortfolioMedia, int>       mediaRepository,
        IRepository<Product, int>                   productRepository)
    {
        _repository            = repository;
        _certificateRepository = certificateRepository;
        _downloadRepository    = downloadRepository;
        _productLineRepository = productLineRepository;
        _projectRepository     = projectRepository;
        _mediaRepository       = mediaRepository;
        _productRepository     = productRepository;
    }

    // ── READ ──────────────────────────────────────────────────────────────

    public async Task<BrandPortfolioDto> GetAsync(int id)
    {
        var entity = await _repository.GetAsync(id);
        return await MapToDtoAsync(entity);
    }

    public async Task<PagedResultDto<BrandPortfolioListDto>> GetListAsync(GetBrandPortfolioListDto input)
    {
        var query = await _repository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim();
            query = query.Where(b =>
                (b.Name    != null && b.Name.Contains(filter)) ||
                (b.Slug    != null && b.Slug.Contains(filter)) ||
                (b.Tagline != null && b.Tagline.Contains(filter)));
        }

        if (input.IsPublished.HasValue)
            query = query.Where(b => b.IsPublished == input.IsPublished.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.OrderBy(b => b.DisplayOrder)
                 .ThenBy(b => b.Id)
                 .Skip(input.SkipCount)
                 .Take(input.MaxResultCount));

        var ids = items.Select(b => b.Id).ToList();
        var certCounts = await CountCertificatesAsync(ids, visibleOnly: false);
        var lineCounts = await CountProductLinesAsync(ids, visibleOnly: false);

        return new PagedResultDto<BrandPortfolioListDto>(
            totalCount,
            items.Select(b => MapToListDto(
                b,
                certCounts.GetValueOrDefault(b.Id),
                lineCounts.GetValueOrDefault(b.Id))).ToList());
    }

    [AllowAnonymous]
    public async Task<BrandPortfolioDto> GetBySlugAsync(string slug)
    {
        // A page that is missing, retired or still a draft all look the same from the shop: 404.
        var entity = await _repository.FindAsync(b => b.Slug == slug && b.IsPublished && b.IsActive)
            ?? throw new EntityNotFoundException($"No brand page is published at '{slug}'.");

        return await MapToDtoAsync(entity);
    }

    [AllowAnonymous]
    public async Task<ListResultDto<BrandPortfolioListDto>> GetPublishedAsync()
    {
        var query = await _repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query.Where(b => b.IsPublished && b.IsActive)
                 .OrderBy(b => b.DisplayOrder)
                 .ThenBy(b => b.Id));

        var ids = items.Select(b => b.Id).ToList();
        var certCounts = await CountCertificatesAsync(ids, visibleOnly: true);
        var lineCounts = await CountProductLinesAsync(ids, visibleOnly: true);

        return new ListResultDto<BrandPortfolioListDto>(
            items.Select(b => MapToListDto(
                b,
                certCounts.GetValueOrDefault(b.Id),
                lineCounts.GetValueOrDefault(b.Id))).ToList());
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.BrandPortfolios.Create)]
    public async Task<BrandPortfolioDto> CreateAsync(CreateUpdateBrandPortfolioDto input)
    {
        await CheckSlugIsFreeAsync(input.Slug, null);

        var entity = new BrandPortfolio();
        ApplyInput(entity, input);
        await _repository.InsertAsync(entity, autoSave: true);

        await InsertChildrenAsync(entity.Id, input);

        return await MapToDtoAsync(entity);
    }

    [Authorize(DymoEnergyPermissions.BrandPortfolios.Edit)]
    public async Task<BrandPortfolioDto> UpdateAsync(int id, CreateUpdateBrandPortfolioDto input)
    {
        var entity = await _repository.GetAsync(id);
        await CheckSlugIsFreeAsync(input.Slug, id);

        ApplyInput(entity, input);
        await _repository.UpdateAsync(entity, autoSave: true);

        await SyncChildrenAsync(
            _certificateRepository, input.Certificates,
            c => c.BrandPortfolioId == id, d => d.Id,
            d => MapToCertificate(d, id), ApplyCertificateInput);

        await SyncChildrenAsync(
            _downloadRepository, input.Downloads,
            c => c.BrandPortfolioId == id, d => d.Id,
            d => MapToDownload(d, id), ApplyDownloadInput);

        await SyncChildrenAsync(
            _productLineRepository, input.ProductLines,
            c => c.BrandPortfolioId == id, d => d.Id,
            d => MapToProductLine(d, id), ApplyProductLineInput);

        await SyncChildrenAsync(
            _projectRepository, input.Projects,
            c => c.BrandPortfolioId == id, d => d.Id,
            d => MapToProject(d, id), ApplyProjectInput);

        await SyncChildrenAsync(
            _mediaRepository, input.Media,
            c => c.BrandPortfolioId == id, d => d.Id,
            d => MapToMedia(d, id), ApplyMediaInput);

        return await MapToDtoAsync(entity);
    }

    [Authorize(DymoEnergyPermissions.BrandPortfolios.Delete)]
    public async Task DeleteAsync(int id)
    {
        // Children are plain entities, so they are cleared explicitly before the page goes.
        await DeleteChildrenAsync(_certificateRepository, c => c.BrandPortfolioId == id);
        await DeleteChildrenAsync(_downloadRepository,    c => c.BrandPortfolioId == id);
        await DeleteChildrenAsync(_productLineRepository, c => c.BrandPortfolioId == id);
        await DeleteChildrenAsync(_projectRepository,     c => c.BrandPortfolioId == id);
        await DeleteChildrenAsync(_mediaRepository,       c => c.BrandPortfolioId == id);

        await _repository.DeleteAsync(id, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.BrandPortfolios.Edit)]
    public async Task<BrandPortfolioDto> SetPublishedAsync(int id, bool isPublished)
    {
        var entity = await _repository.GetAsync(id);

        if (isPublished && string.IsNullOrWhiteSpace(entity.Name))
            throw new UserFriendlyException("Give the brand a name before the page goes public.");

        entity.IsPublished = isPublished;
        await _repository.UpdateAsync(entity, autoSave: true);

        return await MapToDtoAsync(entity);
    }

    // ── SLUG ──────────────────────────────────────────────────────────────

    private async Task CheckSlugIsFreeAsync(string slug, int? exceptId)
    {
        var query = await _repository.GetQueryableAsync();
        var taken = await AsyncExecuter.AnyAsync(
            query.Where(b => b.Slug == slug && (exceptId == null || b.Id != exceptId.Value)));

        if (taken)
            throw new UserFriendlyException($"Another brand page already uses the web address '{slug}'.");
    }

    // ── CHILD SYNC ────────────────────────────────────────────────────────

    private async Task InsertChildrenAsync(int brandId, CreateUpdateBrandPortfolioDto input)
    {
        if (input.Certificates.Count > 0)
            await _certificateRepository.InsertManyAsync(
                input.Certificates.Select(d => MapToCertificate(d, brandId)).ToList(), autoSave: true);

        if (input.Downloads.Count > 0)
            await _downloadRepository.InsertManyAsync(
                input.Downloads.Select(d => MapToDownload(d, brandId)).ToList(), autoSave: true);

        if (input.ProductLines.Count > 0)
            await _productLineRepository.InsertManyAsync(
                input.ProductLines.Select(d => MapToProductLine(d, brandId)).ToList(), autoSave: true);

        if (input.Projects.Count > 0)
            await _projectRepository.InsertManyAsync(
                input.Projects.Select(d => MapToProject(d, brandId)).ToList(), autoSave: true);

        if (input.Media.Count > 0)
            await _mediaRepository.InsertManyAsync(
                input.Media.Select(d => MapToMedia(d, brandId)).ToList(), autoSave: true);
    }

    /// <summary>
    /// Brings a child table in line with what the editor posted: rows the editor dropped are
    /// deleted, rows carrying an id are updated, and rows without one are inserted.
    /// </summary>
    private async Task SyncChildrenAsync<TEntity, TDto>(
        IRepository<TEntity, int>  repository,
        List<TDto>                 inputRows,
        Expression<Func<TEntity, bool>> brandFilter,
        Func<TDto, int?>           idOf,
        Func<TDto, TEntity>        toNewEntity,
        Action<TEntity, TDto>      applyInput)
        where TEntity : class, Volo.Abp.Domain.Entities.IEntity<int>
    {
        var query   = await repository.GetQueryableAsync();
        var current = await AsyncExecuter.ToListAsync(query.Where(brandFilter));

        var withId  = inputRows.Where(r => idOf(r) is > 0).ToList();
        var fresh   = inputRows.Where(r => !(idOf(r) is > 0)).ToList();
        var keepIds = withId.Select(r => idOf(r)!.Value).ToHashSet();

        var toDelete = current.Where(c => !keepIds.Contains(c.Id)).ToList();
        if (toDelete.Count > 0)
            await repository.DeleteManyAsync(toDelete, autoSave: true);

        var toUpdate = new List<TEntity>();
        foreach (var dto in withId)
        {
            var existing = current.FirstOrDefault(c => c.Id == idOf(dto)!.Value);
            if (existing == null) continue;
            applyInput(existing, dto);
            toUpdate.Add(existing);
        }
        if (toUpdate.Count > 0)
            await repository.UpdateManyAsync(toUpdate, autoSave: true);

        var toInsert = fresh.Select(toNewEntity).ToList();
        if (toInsert.Count > 0)
            await repository.InsertManyAsync(toInsert, autoSave: true);
    }

    private async Task DeleteChildrenAsync<TEntity>(
        IRepository<TEntity, int> repository,
        Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, Volo.Abp.Domain.Entities.IEntity<int>
    {
        await repository.DeleteAsync(predicate, autoSave: true);
    }

    /// <param name="visibleOnly">
    /// True for the storefront, which must not advertise rows the admin switched off;
    /// false for the admin list, where the total number of rows is the useful number.
    /// </param>
    private async Task<Dictionary<int, int>> CountCertificatesAsync(List<int> brandIds, bool visibleOnly)
    {
        if (brandIds.Count == 0) return new Dictionary<int, int>();

        var query = await _certificateRepository.GetQueryableAsync();
        var rows  = await AsyncExecuter.ToListAsync(
            query.Where(c => brandIds.Contains(c.BrandPortfolioId) && (!visibleOnly || c.IsVisible))
                 .Select(c => c.BrandPortfolioId));

        return rows.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Dictionary<int, int>> CountProductLinesAsync(List<int> brandIds, bool visibleOnly)
    {
        if (brandIds.Count == 0) return new Dictionary<int, int>();

        var query = await _productLineRepository.GetQueryableAsync();
        var rows  = await AsyncExecuter.ToListAsync(
            query.Where(l => brandIds.Contains(l.BrandPortfolioId) && (!visibleOnly || l.IsVisible))
                 .Select(l => l.BrandPortfolioId));

        return rows.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    // ── MAPPING: input → entity ───────────────────────────────────────────

    private static void ApplyInput(BrandPortfolio entity, CreateUpdateBrandPortfolioDto input)
    {
        entity.PortalId           = input.PortalId;
        entity.Name               = input.Name?.Trim();
        entity.Slug               = input.Slug?.Trim().ToLowerInvariant();
        entity.Tagline            = input.Tagline;
        entity.LogoUrl            = input.LogoUrl;
        entity.ShortIntro         = input.ShortIntro;
        entity.DistributorSince   = input.DistributorSince;
        entity.BrandColor         = input.BrandColor;
        entity.StoryHeadline      = input.StoryHeadline;
        entity.HeroKind           = input.HeroKind;
        entity.HeroMediaUrl       = input.HeroMediaUrl;
        entity.HeroCaption        = input.HeroCaption;
        entity.HeroAutoplayMuted  = input.HeroAutoplayMuted;
        entity.HeroUsedForSharing = input.HeroUsedForSharing;
        entity.CtaHeadline        = input.CtaHeadline;
        entity.CtaSubline         = input.CtaSubline;
        entity.MetaTitle          = input.MetaTitle;
        entity.MetaDescription    = input.MetaDescription;
        entity.ContentJson        = input.ContentJson;
        entity.IsPublished        = input.IsPublished;
        entity.IsActive           = input.IsActive;
        entity.DisplayOrder       = input.DisplayOrder;
    }

    private static BrandPortfolioCertificate MapToCertificate(CreateUpdateBrandPortfolioCertificateDto d, int brandId)
    {
        var e = new BrandPortfolioCertificate { BrandPortfolioId = brandId };
        ApplyCertificateInput(e, d);
        return e;
    }

    private static void ApplyCertificateInput(BrandPortfolioCertificate e, CreateUpdateBrandPortfolioCertificateDto d)
    {
        e.Mark              = d.Mark;
        e.Covers            = d.Covers;
        e.CertificateNumber = d.CertificateNumber;
        e.NoExpiry          = d.NoExpiry;
        e.ExpiresOn         = d.NoExpiry ? null : d.ExpiresOn;
        e.FileUrl           = d.FileUrl;
        e.FileSize          = d.FileSize;
        e.IsVisible         = d.IsVisible;
        e.DisplayOrder      = d.DisplayOrder;
    }

    private static BrandPortfolioDownload MapToDownload(CreateUpdateBrandPortfolioDownloadDto d, int brandId)
    {
        var e = new BrandPortfolioDownload { BrandPortfolioId = brandId };
        ApplyDownloadInput(e, d);
        return e;
    }

    private static void ApplyDownloadInput(BrandPortfolioDownload e, CreateUpdateBrandPortfolioDownloadDto d)
    {
        e.Title        = d.Title;
        e.Kind         = d.Kind;
        e.FileUrl      = d.FileUrl;
        e.FileSize     = d.FileSize;
        e.DealersOnly  = d.DealersOnly;
        e.IsActive     = d.IsActive;
        e.DisplayOrder = d.DisplayOrder;
    }

    private static BrandPortfolioProductLine MapToProductLine(CreateUpdateBrandPortfolioProductLineDto d, int brandId)
    {
        var e = new BrandPortfolioProductLine { BrandPortfolioId = brandId };
        ApplyProductLineInput(e, d);
        return e;
    }

    private static void ApplyProductLineInput(BrandPortfolioProductLine e, CreateUpdateBrandPortfolioProductLineDto d)
    {
        e.Name         = d.Name;
        e.Blurb        = d.Blurb;
        e.CategoryId   = d.CategoryId;
        e.IconKey      = d.IconKey;
        e.IsVisible    = d.IsVisible;
        e.DisplayOrder = d.DisplayOrder;
    }

    private static BrandPortfolioProject MapToProject(CreateUpdateBrandPortfolioProjectDto d, int brandId)
    {
        var e = new BrandPortfolioProject { BrandPortfolioId = brandId };
        ApplyProjectInput(e, d);
        return e;
    }

    private static void ApplyProjectInput(BrandPortfolioProject e, CreateUpdateBrandPortfolioProjectDto d)
    {
        e.ProjectId     = d.ProjectId;
        e.Title         = d.Title;
        e.Subtitle      = d.Subtitle;
        e.ImageUrl      = d.ImageUrl;
        e.ConsentStatus = d.ConsentStatus;
        e.DisplayOrder  = d.DisplayOrder;
    }

    private static BrandPortfolioMedia MapToMedia(CreateUpdateBrandPortfolioMediaDto d, int brandId)
    {
        var e = new BrandPortfolioMedia { BrandPortfolioId = brandId };
        ApplyMediaInput(e, d);
        return e;
    }

    private static void ApplyMediaInput(BrandPortfolioMedia e, CreateUpdateBrandPortfolioMediaDto d)
    {
        e.ImageUrl     = d.ImageUrl;
        e.Caption      = d.Caption;
        e.AltText      = d.AltText;
        e.IsActive     = d.IsActive;
        e.DisplayOrder = d.DisplayOrder;
    }

    // ── MAPPING: entity → DTO ─────────────────────────────────────────────

    private async Task<BrandPortfolioDto> MapToDtoAsync(BrandPortfolio b)
    {
        var id = b.Id;
        var certificates = await LoadChildrenAsync(_certificateRepository, c => c.BrandPortfolioId == id, c => c.DisplayOrder);
        var downloads    = await LoadChildrenAsync(_downloadRepository,    c => c.BrandPortfolioId == id, c => c.DisplayOrder);
        var productLines = await LoadChildrenAsync(_productLineRepository, c => c.BrandPortfolioId == id, c => c.DisplayOrder);
        var projects     = await LoadChildrenAsync(_projectRepository,     c => c.BrandPortfolioId == id, c => c.DisplayOrder);
        var media        = await LoadChildrenAsync(_mediaRepository,       c => c.BrandPortfolioId == id, c => c.DisplayOrder);

        var lineStats = await LoadLineStatsAsync(productLines);

        return new BrandPortfolioDto
        {
            Id                   = b.Id,
            CreationTime         = b.CreationTime,
            CreatorId            = b.CreatorId,
            LastModificationTime = b.LastModificationTime,
            LastModifierId       = b.LastModifierId,
            IsDeleted            = b.IsDeleted,
            DeletionTime         = b.DeletionTime,
            DeleterId            = b.DeleterId,
            PortalId             = b.PortalId,
            Name                 = b.Name,
            Slug                 = b.Slug,
            Tagline              = b.Tagline,
            LogoUrl              = b.LogoUrl,
            ShortIntro           = b.ShortIntro,
            DistributorSince     = b.DistributorSince,
            BrandColor           = b.BrandColor,
            StoryHeadline        = b.StoryHeadline,
            HeroKind             = b.HeroKind,
            HeroMediaUrl         = b.HeroMediaUrl,
            HeroCaption          = b.HeroCaption,
            HeroAutoplayMuted    = b.HeroAutoplayMuted,
            HeroUsedForSharing   = b.HeroUsedForSharing,
            CtaHeadline          = b.CtaHeadline,
            CtaSubline           = b.CtaSubline,
            MetaTitle            = b.MetaTitle,
            MetaDescription      = b.MetaDescription,
            ContentJson          = b.ContentJson,
            IsPublished          = b.IsPublished,
            IsActive             = b.IsActive,
            DisplayOrder         = b.DisplayOrder,

            Certificates = certificates.Select(c => new BrandPortfolioCertificateDto
            {
                Id                = c.Id,
                BrandPortfolioId  = c.BrandPortfolioId,
                Mark              = c.Mark,
                Covers            = c.Covers,
                CertificateNumber = c.CertificateNumber,
                NoExpiry          = c.NoExpiry,
                ExpiresOn         = c.ExpiresOn,
                FileUrl           = c.FileUrl,
                FileSize          = c.FileSize,
                IsVisible         = c.IsVisible,
                DisplayOrder      = c.DisplayOrder
            }).ToList(),

            Downloads = downloads.Select(d => new BrandPortfolioDownloadDto
            {
                Id               = d.Id,
                BrandPortfolioId = d.BrandPortfolioId,
                Title            = d.Title,
                Kind             = d.Kind,
                FileUrl          = d.FileUrl,
                FileSize         = d.FileSize,
                DealersOnly      = d.DealersOnly,
                IsActive         = d.IsActive,
                DisplayOrder     = d.DisplayOrder
            }).ToList(),

            ProductLines = productLines.Select(l => new BrandPortfolioProductLineDto
            {
                Id               = l.Id,
                BrandPortfolioId = l.BrandPortfolioId,
                Name             = l.Name,
                Blurb            = l.Blurb,
                CategoryId       = l.CategoryId,
                IconKey          = l.IconKey,
                IsVisible        = l.IsVisible,
                DisplayOrder     = l.DisplayOrder,
                ProductCount     = lineStats.TryGetValue(l.Id, out var s) ? s.Count : 0,
                FromPrice        = lineStats.TryGetValue(l.Id, out var s2) ? s2.FromPrice : null
            }).ToList(),

            Projects = projects.Select(p => new BrandPortfolioProjectDto
            {
                Id               = p.Id,
                BrandPortfolioId = p.BrandPortfolioId,
                ProjectId        = p.ProjectId,
                Title            = p.Title,
                Subtitle         = p.Subtitle,
                ImageUrl         = p.ImageUrl,
                ConsentStatus    = p.ConsentStatus,
                DisplayOrder     = p.DisplayOrder
            }).ToList(),

            Media = media.Select(m => new BrandPortfolioMediaDto
            {
                Id               = m.Id,
                BrandPortfolioId = m.BrandPortfolioId,
                ImageUrl         = m.ImageUrl,
                Caption          = m.Caption,
                AltText          = m.AltText,
                IsActive         = m.IsActive,
                DisplayOrder     = m.DisplayOrder
            }).ToList()
        };
    }

    private static BrandPortfolioListDto MapToListDto(BrandPortfolio b, int certificateCount, int productLineCount) => new()
    {
        Id                   = b.Id,
        CreationTime         = b.CreationTime,
        CreatorId            = b.CreatorId,
        LastModificationTime = b.LastModificationTime,
        LastModifierId       = b.LastModifierId,
        Name                 = b.Name,
        Slug                 = b.Slug,
        Tagline              = b.Tagline,
        LogoUrl              = b.LogoUrl,
        BrandColor           = b.BrandColor,
        IsPublished          = b.IsPublished,
        IsActive             = b.IsActive,
        DisplayOrder         = b.DisplayOrder,
        CertificateCount     = certificateCount,
        ProductLineCount     = productLineCount,
        CompletenessPercent  = Completeness(b, certificateCount, productLineCount)
    };

    /// <summary>
    /// The share of the page's blocks that carry something. Empty blocks are hidden on the
    /// shop rather than shown blank, so this is what the admin list reports as "filled in".
    /// </summary>
    private static int Completeness(BrandPortfolio b, int certificateCount, int productLineCount)
    {
        var filled = new[]
        {
            !string.IsNullOrWhiteSpace(b.Name),
            !string.IsNullOrWhiteSpace(b.Tagline),
            !string.IsNullOrWhiteSpace(b.LogoUrl),
            !string.IsNullOrWhiteSpace(b.ShortIntro),
            !string.IsNullOrWhiteSpace(b.DistributorSince),
            !string.IsNullOrWhiteSpace(b.BrandColor),
            !string.IsNullOrWhiteSpace(b.StoryHeadline),
            !string.IsNullOrWhiteSpace(b.HeroMediaUrl),
            !string.IsNullOrWhiteSpace(b.CtaHeadline),
            !string.IsNullOrWhiteSpace(b.MetaTitle),
            !string.IsNullOrWhiteSpace(b.MetaDescription),
            HasContent(b.ContentJson),
            certificateCount > 0,
            productLineCount > 0
        };

        return (int)Math.Round(filled.Count(f => f) * 100.0 / filled.Length);
    }

    private static bool HasContent(string? json) =>
        !string.IsNullOrWhiteSpace(json) && json.Trim() is not ("{}" or "[]" or "null");

    private async Task<List<TEntity>> LoadChildrenAsync<TEntity>(
        IRepository<TEntity, int>       repository,
        Expression<Func<TEntity, bool>> brandFilter,
        Func<TEntity, int>              orderBy)
        where TEntity : class, Volo.Abp.Domain.Entities.IEntity<int>
    {
        var query = await repository.GetQueryableAsync();
        var rows  = await AsyncExecuter.ToListAsync(query.Where(brandFilter));
        return rows.OrderBy(orderBy).ThenBy(r => r.Id).ToList();
    }

    /// <summary>Live product count and cheapest price for the lines that point at a category.</summary>
    private async Task<Dictionary<int, (int Count, double? FromPrice)>> LoadLineStatsAsync(
        List<BrandPortfolioProductLine> lines)
    {
        var categoryIds = lines.Where(l => l.CategoryId.HasValue)
                               .Select(l => l.CategoryId!.Value)
                               .Distinct()
                               .ToList();

        if (categoryIds.Count == 0)
            return new Dictionary<int, (int, double?)>();

        var productQuery = await _productRepository.GetQueryableAsync();
        var products = await AsyncExecuter.ToListAsync(
            productQuery.Where(p => p.IsActive && categoryIds.Contains(p.CategoryId))
                        .Select(p => new { p.CategoryId, p.Price, p.DiscountPrice }));

        var byCategory = products
            .GroupBy(p => p.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => (Count: g.Count(), FromPrice: (double?)g.Min(p => p.DiscountPrice ?? p.Price)));

        return lines
            .Where(l => l.CategoryId.HasValue && byCategory.ContainsKey(l.CategoryId!.Value))
            .ToDictionary(l => l.Id, l => byCategory[l.CategoryId!.Value]);
    }
}
