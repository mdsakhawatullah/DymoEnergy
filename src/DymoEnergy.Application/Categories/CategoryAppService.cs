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

namespace DymoEnergy.Categories;

[Authorize(DymoEnergyPermissions.Categories.Default)]
public class CategoryAppService : ApplicationService, ICategoryAppService
{
    private const int ShowcaseProductLimit = 8;

    private readonly IRepository<Category, int>      _categoryRepository;
    private readonly IRepository<CategoryImage, int> _imageRepository;
    private readonly IRepository<Product, int>        _productRepository;

    public CategoryAppService(
        IRepository<Category, int>      categoryRepository,
        IRepository<CategoryImage, int> imageRepository,
        IRepository<Product, int>        productRepository)
    {
        _categoryRepository = categoryRepository;
        _imageRepository     = imageRepository;
        _productRepository   = productRepository;
    }

    // ── READ ─────────────────────────────────────────────────────────────

    /// <summary>Returns a single category with all its images.</summary>
    [AllowAnonymous]
    public async Task<CategoryDto> GetAsync(int id)
    {
        var query = await _categoryRepository.WithDetailsAsync(c => c.Images);

        var category = await AsyncExecuter.FirstOrDefaultAsync(query.Where(c => c.Id == id))
            ?? throw new EntityNotFoundException(typeof(Category), id);

        return MapToDto(category);
    }

    /// <summary>Returns a single category by its URL slug with all its images.</summary>
    [AllowAnonymous]
    public async Task<CategoryDto> GetBySlugAsync(string slug)
    {
        var normalised = slug.ToLowerInvariant().Trim();
        var query      = await _categoryRepository.WithDetailsAsync(c => c.Images);

        var category = await AsyncExecuter.FirstOrDefaultAsync(query.Where(c => c.Slug == normalised))
            ?? throw new UserFriendlyException($"Category with slug '{slug}' not found.");

        return MapToDto(category);
    }

    /// <summary>
    /// Paged, filtered list — no JOIN to the Images table so the SQL stays lean.
    /// All filtering is translated to SQL; no in-memory evaluation.
    /// </summary>
    [AllowAnonymous]
    public async Task<DymoPagedResultDto<CategoryDto>> GetListDataAsync(CategoryFilterDto input)
    {
        // Plain IQueryable — no Include → single-table SQL query
        var query = await _categoryRepository.GetQueryableAsync();

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

        var categories = await AsyncExecuter.ToListAsync(query);

        // Images are not loaded for the list — MapToDto returns empty Images list
        return new DymoPagedResultDto<CategoryDto>(totalCount, categories.Select(MapToDto).ToList());
    }

    [AllowAnonymous]
    public async Task<IEnumerable<SelectListDto>> GetSelectListAsync()
    {
        var query = (await _categoryRepository.GetQueryableAsync())
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
    /// Home-page showcase: every published category that has active products, each with
    /// its thumbnail and first <see cref="ShowcaseProductLimit"/> products. Single SQL query.
    /// </summary>
    [AllowAnonymous]
    public async Task<List<HomeCategoryShowcaseDto>> GetHomeShowcaseAsync()
    {
        var products = (await _productRepository.GetQueryableAsync())
            .Where(p => p.IsActive);

        var query = (await _categoryRepository.GetQueryableAsync())
            .Where(c => c.IsPublished && products.Any(p => p.CategoryId == c.Id))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new HomeCategoryShowcaseDto
            {
                Id                = c.Id,
                Name              = c.Name,
                Slug              = c.Slug,
                ThumbnailImageUrl = c.ThumbnailImageUrl ?? c.PrimaryBackgroundImageUrl,
                AccentColor       = c.AccentColor,
                TotalProductCount = products.Count(p => p.CategoryId == c.Id),
                Products = products
                    .Where(p => p.CategoryId == c.Id)
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

    [Authorize(DymoEnergyPermissions.Categories.Create)]
    public async Task<CategoryDto> CreateCategoryDataAsync(CreateUpdateCategoryDto input)
    {
        if (!string.IsNullOrWhiteSpace(input.Slug))
            await EnsureSlugIsUniqueAsync(input.Slug);

        var category = new Category();
        ApplyInput(category, input);

        // Insert category first so EF assigns the auto-increment Id
        await _categoryRepository.InsertAsync(category, autoSave: true);

        // Bulk-insert all images
        if (input.Images.Count > 0)
        {
            var images = input.Images
                .Select(dto => MapToImage(dto, category.Id))
                .ToList();

            await _imageRepository.InsertManyAsync(images, autoSave: true);
            category.Images = images;
        }

        return MapToDto(category);
    }

    [Authorize(DymoEnergyPermissions.Categories.Edit)]
    public async Task<CategoryDto> UpdateAsync(int id, CreateUpdateCategoryDto input)
    {
        // Load category WITHOUT images — scalar update only
        var category = await _categoryRepository.GetAsync(id);

        if (!string.IsNullOrWhiteSpace(input.Slug) &&
            !string.Equals(category.Slug, input.Slug, StringComparison.OrdinalIgnoreCase))
            await EnsureSlugIsUniqueAsync(input.Slug);

        ApplyInput(category, input);
        await _categoryRepository.UpdateAsync(category, autoSave: true);

        // Load current images from DB (separate, targeted SQL query)
        var imageQuery   = await _imageRepository.GetQueryableAsync();
        var currentImages = await AsyncExecuter.ToListAsync(
            imageQuery.Where(i => i.CategoryId == id));

        // Classify input images
        var inputWithId = input.Images.Where(i => i.Id is > 0).ToList();
        var inputNewIds = input.Images.Where(i => !(i.Id is > 0)).ToList();
        var keepIds     = inputWithId.Select(i => i.Id!.Value).ToHashSet();

        // ── 1. DeleteManyAsync — images removed by the admin ─────────────
        var toDelete = currentImages.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _imageRepository.DeleteManyAsync(toDelete, autoSave: true);

        // ── 2. UpdateManyAsync — images that already exist ────────────────
        var toUpdate = new List<CategoryImage>();
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
            .Select(dto => MapToImage(dto, category.Id))
            .ToList();
        if (toInsert.Count > 0)
            await _imageRepository.InsertManyAsync(toInsert, autoSave: true);

        // Build DTO from in-memory state (no extra DB round-trip)
        category.Images = toUpdate.Concat(toInsert)
            .OrderBy(i => i.DisplayOrder)
            .ToList();

        return MapToDto(category);
    }

    [Authorize(DymoEnergyPermissions.Categories.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _categoryRepository.DeleteAsync(id, autoSave: true);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static void ApplyInput(Category c, CreateUpdateCategoryDto input)
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

    private static CategoryImage MapToImage(CreateUpdateCategoryImageDto dto, int categoryId) => new()
    {
        CategoryId  = categoryId,
        ImageUrl     = dto.ImageUrl,
        ImageType    = dto.ImageType,
        Title        = dto.Title,
        AltText      = dto.AltText,
        DisplayOrder = dto.DisplayOrder,
        IsActive     = dto.IsActive
    };

    private static void ApplyImageInput(CategoryImage img, CreateUpdateCategoryImageDto dto)
    {
        img.ImageUrl     = dto.ImageUrl;
        img.ImageType    = dto.ImageType;
        img.Title        = dto.Title;
        img.AltText      = dto.AltText;
        img.DisplayOrder = dto.DisplayOrder;
        img.IsActive     = dto.IsActive;
    }

    private static CategoryDto MapToDto(Category c) => new()
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
            .Select(i => new CategoryImageDto
            {
                Id           = i.Id,
                CategoryId  = i.CategoryId,
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
        var query      = await _categoryRepository.GetQueryableAsync();
        var exists     = await AsyncExecuter.AnyAsync(query.Where(c => c.Slug == normalised));
        if (exists)
            throw new BusinessException(message: $"A category with slug '{slug}' already exists.");
    }
}
