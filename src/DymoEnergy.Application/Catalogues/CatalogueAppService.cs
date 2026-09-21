using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Products;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Catalogues;

[Authorize(DymoEnergyPermissions.Catalogues.Default)]
public class CatalogueAppService : ApplicationService, ICatalogueAppService
{
    private const int ShowcaseProductLimit = 8;

    private readonly IRepository<Catalogue, int>      _catalogueRepository;
    private readonly IRepository<CatalogueImage, int> _imageRepository;
    private readonly IRepository<Product, int>        _productRepository;

    public CatalogueAppService(
        IRepository<Catalogue, int>      catalogueRepository,
        IRepository<CatalogueImage, int> imageRepository,
        IRepository<Product, int>        productRepository)
    {
        _catalogueRepository = catalogueRepository;
        _imageRepository     = imageRepository;
        _productRepository   = productRepository;
    }

    // ── READ ─────────────────────────────────────────────────────────────

    /// <summary>Returns a single catalogue with all its images.</summary>
    [AllowAnonymous]
    public async Task<CatalogueDto> GetAsync(int id)
    {
        var query = await _catalogueRepository.WithDetailsAsync(c => c.Images);

        var catalogue = await AsyncExecuter.FirstOrDefaultAsync(query.Where(c => c.Id == id))
            ?? throw new EntityNotFoundException(typeof(Catalogue), id);

        return MapToDto(catalogue);
    }

    /// <summary>Returns a single catalogue by its URL slug with all its images.</summary>
    [AllowAnonymous]
    public async Task<CatalogueDto> GetBySlugAsync(string slug)
    {
        var normalised = slug.ToLowerInvariant().Trim();
        var query      = await _catalogueRepository.WithDetailsAsync(c => c.Images);

        var catalogue = await AsyncExecuter.FirstOrDefaultAsync(query.Where(c => c.Slug == normalised))
            ?? throw new UserFriendlyException($"Catalogue with slug '{slug}' not found.");

        return MapToDto(catalogue);
    }

    /// <summary>
    /// Paged, filtered list — no JOIN to the Images table so the SQL stays lean.
    /// All filtering is translated to SQL; no in-memory evaluation.
    /// </summary>
    [AllowAnonymous]
    public async Task<DymoPagedResultDto<CatalogueDto>> GetListDataAsync(CatalogueFilterDto input)
    {
        // Plain IQueryable — no Include → single-table SQL query
        var query = await _catalogueRepository.GetQueryableAsync();

        // ── SQL-level filters ─────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(c =>
                (c.Name        != null && c.Name.Contains(input.Filter)) ||
                (c.Description != null && c.Description.Contains(input.Filter)));

        if (input.IsPublished.HasValue)
            query = query.Where(c => c.IsPublished == input.IsPublished);

        if (input.IsFeatured.HasValue)
            query = query.Where(c => c.IsFeatured == input.IsFeatured);

        if (input.PortalId.HasValue)
            query = query.Where(c => c.PortalId == input.PortalId);

        // COUNT before paging (single SQL COUNT query)
        var totalCount = await AsyncExecuter.CountAsync(query);

        // ── Sort + Page ───────────────────────────────────────────────────
        query = query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var catalogues = await AsyncExecuter.ToListAsync(query);

        // Images are not loaded for the list — MapToDto returns empty Images list
        return new DymoPagedResultDto<CatalogueDto>(totalCount, catalogues.Select(MapToDto).ToList());
    }

    [AllowAnonymous]
    public async Task<IEnumerable<SelectListDto>> GetSelectListAsync()
    {
        var query = (await _catalogueRepository.GetQueryableAsync())
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListDto
            {
                Value       = c.Id,
                DisplayText = c.Name ?? string.Empty,
            });

        return await AsyncExecuter.ToListAsync(query);
    }

    /// <summary>
    /// Home-page showcase: every published catalogue that has active products, each with
    /// its thumbnail and first <see cref="ShowcaseProductLimit"/> products. Single SQL query.
    /// </summary>
    [AllowAnonymous]
    public async Task<List<HomeCatalogueShowcaseDto>> GetHomeShowcaseAsync()
    {
        var products = (await _productRepository.GetQueryableAsync())
            .Where(p => p.IsActive);

        var query = (await _catalogueRepository.GetQueryableAsync())
            .Where(c => c.IsPublished && products.Any(p => p.CatalogueId == c.Id))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new HomeCatalogueShowcaseDto
            {
                Id                = c.Id,
                Name              = c.Name,
                Slug              = c.Slug,
                ThumbnailImageUrl = c.ThumbnailImageUrl ?? c.PrimaryBackgroundImageUrl,
                AccentColor       = c.AccentColor,
                TotalProductCount = products.Count(p => p.CatalogueId == c.Id),
                Products = products
                    .Where(p => p.CatalogueId == c.Id)
                    .OrderBy(p => p.DisplayOrder)
                    .ThenBy(p => p.Name)
                    .Take(ShowcaseProductLimit)
                    .Select(p => new HomeShowcaseProductDto
                    {
                        Id            = p.Id,
                        Name          = p.Name,
                        Price         = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        PrimaryImage  = p.PrimaryImage,
                    })
                    .ToList(),
            });

        return await AsyncExecuter.ToListAsync(query);
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.Catalogues.Create)]
    public async Task<CatalogueDto> CreateCatalogueDataAsync(CreateUpdateCatalogueDto input)
    {
        if (!string.IsNullOrWhiteSpace(input.Slug))
            await EnsureSlugIsUniqueAsync(input.Slug);

        var catalogue = new Catalogue();
        ApplyInput(catalogue, input);

        // Insert catalogue first so EF assigns the auto-increment Id
        await _catalogueRepository.InsertAsync(catalogue, autoSave: true);

        // Bulk-insert all images
        if (input.Images.Count > 0)
        {
            var images = input.Images
                .Select(dto => MapToImage(dto, catalogue.Id))
                .ToList();

            await _imageRepository.InsertManyAsync(images, autoSave: true);
            catalogue.Images = images;
        }

        return MapToDto(catalogue);
    }

    [Authorize(DymoEnergyPermissions.Catalogues.Edit)]
    public async Task<CatalogueDto> UpdateAsync(int id, CreateUpdateCatalogueDto input)
    {
        // Load catalogue WITHOUT images — scalar update only
        var catalogue = await _catalogueRepository.GetAsync(id);

        if (!string.IsNullOrWhiteSpace(input.Slug) &&
            !string.Equals(catalogue.Slug, input.Slug, StringComparison.OrdinalIgnoreCase))
            await EnsureSlugIsUniqueAsync(input.Slug);

        ApplyInput(catalogue, input);
        await _catalogueRepository.UpdateAsync(catalogue, autoSave: true);

        // Load current images from DB (separate, targeted SQL query)
        var imageQuery   = await _imageRepository.GetQueryableAsync();
        var currentImages = await AsyncExecuter.ToListAsync(
            imageQuery.Where(i => i.CatalogueId == id));

        // Classify input images
        var inputWithId = input.Images.Where(i => i.Id is > 0).ToList();
        var inputNewIds = input.Images.Where(i => !(i.Id is > 0)).ToList();
        var keepIds     = inputWithId.Select(i => i.Id!.Value).ToHashSet();

        // ── 1. DeleteManyAsync — images removed by the admin ─────────────
        var toDelete = currentImages.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _imageRepository.DeleteManyAsync(toDelete, autoSave: true);

        // ── 2. UpdateManyAsync — images that already exist ────────────────
        var toUpdate = new List<CatalogueImage>();
        foreach (var dto in inputWithId)
        {
            var existing = currentImages.FirstOrDefault(i => i.Id == dto.Id);
            if (existing == null) continue;
            ApplyImageInput(existing, dto);
            toUpdate.Add(existing);
        }
        if (toUpdate.Count > 0)
            await _imageRepository.UpdateManyAsync(toUpdate, autoSave: true);

        // ── 3. InsertManyAsync — new images added by the admin ────────────
        var toInsert = inputNewIds
            .Select(dto => MapToImage(dto, catalogue.Id))
            .ToList();
        if (toInsert.Count > 0)
            await _imageRepository.InsertManyAsync(toInsert, autoSave: true);

        // Build DTO from in-memory state (no extra DB round-trip)
        catalogue.Images = toUpdate.Concat(toInsert)
            .OrderBy(i => i.DisplayOrder)
            .ToList();

        return MapToDto(catalogue);
    }

    [Authorize(DymoEnergyPermissions.Catalogues.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _catalogueRepository.DeleteAsync(id, autoSave: true);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static void ApplyInput(Catalogue c, CreateUpdateCatalogueDto input)
    {
        c.PortalId                  = input.PortalId;
        c.Name                      = input.Name;
        c.Slug                      = input.Slug?.ToLowerInvariant().Trim();
        c.Description               = input.Description;
        c.LongDescription           = input.LongDescription;
        c.HeroTitle                 = input.HeroTitle;
        c.HeroSubtitle              = input.HeroSubtitle;
        c.HeroCtaText               = input.HeroCtaText;
        c.HeroCtaUrl                = input.HeroCtaUrl;
        c.PrimaryBackgroundImageUrl = input.PrimaryBackgroundImageUrl;
        c.ThumbnailImageUrl         = input.ThumbnailImageUrl;
        c.OverlayColor              = input.OverlayColor;
        c.OverlayOpacity            = input.OverlayOpacity;
        c.PrimaryTextColor          = input.PrimaryTextColor;
        c.AccentColor               = input.AccentColor;
        c.SectionBackgroundColor    = input.SectionBackgroundColor;
        c.LayoutType                = input.LayoutType;
        c.IsPublished               = input.IsPublished;
        c.IsFeatured                = input.IsFeatured;
        c.DisplayOrder              = input.DisplayOrder;
        c.MetaTitle                 = input.MetaTitle;
        c.MetaDescription           = input.MetaDescription;
        c.MetaKeywords              = input.MetaKeywords;
    }

    private static CatalogueImage MapToImage(CreateUpdateCatalogueImageDto dto, int catalogueId) => new()
    {
        CatalogueId  = catalogueId,
        ImageUrl     = dto.ImageUrl,
        ImageType    = dto.ImageType,
        Title        = dto.Title,
        AltText      = dto.AltText,
        DisplayOrder = dto.DisplayOrder,
        IsActive     = dto.IsActive
    };

    private static void ApplyImageInput(CatalogueImage img, CreateUpdateCatalogueImageDto dto)
    {
        img.ImageUrl     = dto.ImageUrl;
        img.ImageType    = dto.ImageType;
        img.Title        = dto.Title;
        img.AltText      = dto.AltText;
        img.DisplayOrder = dto.DisplayOrder;
        img.IsActive     = dto.IsActive;
    }

    private static CatalogueDto MapToDto(Catalogue c) => new()
    {
        Id                        = c.Id,
        CreationTime              = c.CreationTime,
        CreatorId                 = c.CreatorId,
        LastModificationTime      = c.LastModificationTime,
        LastModifierId            = c.LastModifierId,
        IsDeleted                 = c.IsDeleted,
        DeletionTime              = c.DeletionTime,
        DeleterId                 = c.DeleterId,
        PortalId                  = c.PortalId,
        Name                      = c.Name,
        Slug                      = c.Slug,
        Description               = c.Description,
        LongDescription           = c.LongDescription,
        HeroTitle                 = c.HeroTitle,
        HeroSubtitle              = c.HeroSubtitle,
        HeroCtaText               = c.HeroCtaText,
        HeroCtaUrl                = c.HeroCtaUrl,
        PrimaryBackgroundImageUrl = c.PrimaryBackgroundImageUrl,
        ThumbnailImageUrl         = c.ThumbnailImageUrl,
        OverlayColor              = c.OverlayColor,
        OverlayOpacity            = c.OverlayOpacity,
        PrimaryTextColor          = c.PrimaryTextColor,
        AccentColor               = c.AccentColor,
        SectionBackgroundColor    = c.SectionBackgroundColor,
        LayoutType                = c.LayoutType,
        IsPublished               = c.IsPublished,
        IsFeatured                = c.IsFeatured,
        DisplayOrder              = c.DisplayOrder,
        MetaTitle                 = c.MetaTitle,
        MetaDescription           = c.MetaDescription,
        MetaKeywords              = c.MetaKeywords,
        Images = c.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new CatalogueImageDto
            {
                Id           = i.Id,
                CatalogueId  = i.CatalogueId,
                ImageUrl     = i.ImageUrl,
                ImageType    = i.ImageType,
                Title        = i.Title,
                AltText      = i.AltText,
                DisplayOrder = i.DisplayOrder,
                IsActive     = i.IsActive
            })
            .ToList()
    };

    private async Task EnsureSlugIsUniqueAsync(string slug)
    {
        var normalised = slug.ToLowerInvariant().Trim();
        var query      = await _catalogueRepository.GetQueryableAsync();
        var exists     = await AsyncExecuter.AnyAsync(query.Where(c => c.Slug == normalised));
        if (exists)
            throw new BusinessException(message: $"A catalogue with slug '{slug}' already exists.");
    }
}
