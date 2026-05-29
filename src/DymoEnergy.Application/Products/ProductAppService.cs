using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Products;

[Authorize(DymoEnergyPermissions.Products.Default)]
public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, int>      _productRepository;
    private readonly IRepository<ProductImage, int> _imageRepository;

    public ProductAppService(
        IRepository<Product, int>      productRepository,
        IRepository<ProductImage, int> imageRepository)
    {
        _productRepository = productRepository;
        _imageRepository   = imageRepository;
    }

    // ── READ ─────────────────────────────────────────────────────────────

    [AllowAnonymous]
    public async Task<ProductDto> GetAsync(int id)
    {
        var query = await _productRepository.WithDetailsAsync(p => p.Images);

        var product = await AsyncExecuter.FirstOrDefaultAsync(query.Where(p => p.Id == id))
            ?? throw new EntityNotFoundException(typeof(Product), id);

        return MapToDto(product);
    }

    [AllowAnonymous]
    public async Task<ProductDto> GetBySlugAsync(string slug)
    {
        var normalised = slug.ToLowerInvariant().Trim();
        var query      = await _productRepository.WithDetailsAsync(p => p.Images);

        var product = await AsyncExecuter.FirstOrDefaultAsync(query.Where(p => p.Slug == normalised))
            ?? throw new UserFriendlyException($"Product with slug '{slug}' not found.");

        return MapToDto(product);
    }

    [AllowAnonymous]
    public async Task<DymoPagedResultDto<ProductDto>> GetListDataAsync(ProductFilterDto input)
    {
        var query = await _productRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(p =>
                (p.Name != null && p.Name.Contains(input.Filter)) ||
                (p.Sku  != null && p.Sku.Contains(input.Filter))  ||
                (p.Slug != null && p.Slug.Contains(input.Filter)));

        if (input.Status.HasValue)
            query = query.Where(p => p.Status == input.Status);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var products = await AsyncExecuter.ToListAsync(query);

        return new DymoPagedResultDto<ProductDto>(totalCount, products.Select(MapToDto).ToList());
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.Products.Create)]
    public async Task<ProductDto> CreateProductDataAsync(CreateUpdateProductDto input)
    {
        if (!string.IsNullOrWhiteSpace(input.Slug))
            await EnsureSlugIsUniqueAsync(input.Slug);

        var product = new Product();
        ApplyInput(product, input);

        await _productRepository.InsertAsync(product, autoSave: true);

        if (input.Images.Count > 0)
        {
            var images = input.Images
                .Select(dto => MapToImage(dto, product.Id))
                .ToList();

            await _imageRepository.InsertManyAsync(images, autoSave: true);
            product.Images = images;
        }

        return MapToDto(product);
    }

    [Authorize(DymoEnergyPermissions.Products.Edit)]
    public async Task<ProductDto> UpdateAsync(int id, CreateUpdateProductDto input)
    {
        var product = await _productRepository.GetAsync(id);

        if (!string.IsNullOrWhiteSpace(input.Slug) &&
            !string.Equals(product.Slug, input.Slug, StringComparison.OrdinalIgnoreCase))
            await EnsureSlugIsUniqueAsync(input.Slug);

        ApplyInput(product, input);
        await _productRepository.UpdateAsync(product, autoSave: true);

        var imageQuery    = await _imageRepository.GetQueryableAsync();
        var currentImages = await AsyncExecuter.ToListAsync(
            imageQuery.Where(i => i.ProductId == id));

        var inputWithId = input.Images.Where(i => i.Id is > 0).ToList();
        var inputNew    = input.Images.Where(i => !(i.Id is > 0)).ToList();
        var keepIds     = inputWithId.Select(i => i.Id!.Value).ToHashSet();

        var toDelete = currentImages.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _imageRepository.DeleteManyAsync(toDelete, autoSave: true);

        var toUpdate = new List<ProductImage>();
        foreach (var dto in inputWithId)
        {
            var existing = currentImages.FirstOrDefault(i => i.Id == dto.Id);
            if (existing == null) continue;
            ApplyImageInput(existing, dto);
            toUpdate.Add(existing);
        }
        if (toUpdate.Count > 0)
            await _imageRepository.UpdateManyAsync(toUpdate, autoSave: true);

        var toInsert = inputNew.Select(dto => MapToImage(dto, product.Id)).ToList();
        if (toInsert.Count > 0)
            await _imageRepository.InsertManyAsync(toInsert, autoSave: true);

        product.Images = toUpdate.Concat(toInsert)
            .OrderBy(i => i.DisplayOrder)
            .ToList();

        return MapToDto(product);
    }

    [Authorize(DymoEnergyPermissions.Products.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _productRepository.DeleteAsync(id, autoSave: true);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static void ApplyInput(Product p, CreateUpdateProductDto input)
    {
        p.PortalId      = input.PortalId;
        p.CatalogueId   = input.CatalogueId;
        p.RibbonText    = input.RibbonText;
        p.Name          = input.Name;
        p.Slug          = input.Slug?.ToLowerInvariant().Trim();
        p.Summary       = input.Summary;
        p.Sku           = input.Sku;
        p.Price         = input.Price;
        p.DiscountPrice = input.DiscountPrice;
        p.Weight        = input.Weight;
        p.VendorId      = input.VendorId;
        p.VendorCode    = input.VendorCode;
        p.VendorName    = input.VendorName;
        p.Category1Id   = input.Category1Id;
        p.Category2Id   = input.Category2Id;
        p.Category3Id   = input.Category3Id;
        p.Bundle        = input.Bundle;
        p.BundleCode    = input.BundleCode;
        p.Status        = input.Status;
        p.IsActive      = input.IsActive;
        p.IsFeatured    = input.IsFeatured;
        p.DisplayOrder  = input.DisplayOrder;
        p.StockQuantity = input.StockQuantity;
        p.PrimaryImage  = input.PrimaryImage;
        p.Description   = input.Description;
        p.MetaTitle     = input.MetaTitle;
        p.MetaDescription = input.MetaDescription;
        p.MetaKeywords  = input.MetaKeywords;
    }

    private static ProductImage MapToImage(CreateUpdateProductImageDto dto, int productId) => new()
    {
        ProductId    = productId,
        ImageUrl     = dto.ImageUrl,
        Title        = dto.Title,
        AltText      = dto.AltText,
        DisplayOrder = dto.DisplayOrder,
        IsActive     = dto.IsActive,
    };

    private static void ApplyImageInput(ProductImage img, CreateUpdateProductImageDto dto)
    {
        img.ImageUrl     = dto.ImageUrl;
        img.Title        = dto.Title;
        img.AltText      = dto.AltText;
        img.DisplayOrder = dto.DisplayOrder;
        img.IsActive     = dto.IsActive;
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id                  = p.Id,
        CreationTime        = p.CreationTime,
        CreatorId           = p.CreatorId,
        LastModificationTime = p.LastModificationTime,
        LastModifierId      = p.LastModifierId,
        IsDeleted           = p.IsDeleted,
        DeletionTime        = p.DeletionTime,
        DeleterId           = p.DeleterId,
        PortalId            = p.PortalId,
        CatalogueId         = p.CatalogueId,
        RibbonText          = p.RibbonText,
        Name                = p.Name,
        Slug                = p.Slug,
        Summary             = p.Summary,
        Sku                 = p.Sku,
        Price               = p.Price,
        DiscountPrice       = p.DiscountPrice,
        Weight              = p.Weight,
        VendorId            = p.VendorId,
        VendorCode          = p.VendorCode,
        VendorName          = p.VendorName,
        Category1Id         = p.Category1Id,
        Category2Id         = p.Category2Id,
        Category3Id         = p.Category3Id,
        Bundle              = p.Bundle,
        BundleCode          = p.BundleCode,
        Status              = p.Status,
        IsActive            = p.IsActive,
        IsFeatured          = p.IsFeatured,
        DisplayOrder        = p.DisplayOrder,
        StockQuantity       = p.StockQuantity,
        PrimaryImage        = p.PrimaryImage,
        Description         = p.Description,
        MetaTitle           = p.MetaTitle,
        MetaDescription     = p.MetaDescription,
        MetaKeywords        = p.MetaKeywords,
        Images = p.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageDto
            {
                Id           = i.Id,
                ProductId    = i.ProductId,
                ImageUrl     = i.ImageUrl,
                Title        = i.Title,
                AltText      = i.AltText,
                DisplayOrder = i.DisplayOrder,
                IsActive     = i.IsActive,
            })
            .ToList(),
    };

    private async Task EnsureSlugIsUniqueAsync(string slug)
    {
        var normalised = slug.ToLowerInvariant().Trim();
        var query      = await _productRepository.GetQueryableAsync();
        var exists     = await AsyncExecuter.AnyAsync(query.Where(p => p.Slug == normalised));
        if (exists)
            throw new BusinessException(message: $"A product with slug '{slug}' already exists.");
    }
}
