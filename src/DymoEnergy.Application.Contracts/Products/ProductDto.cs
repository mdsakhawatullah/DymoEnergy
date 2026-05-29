using System.Collections.Generic;
using DymoEnergy.Products;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Products;

public class ProductDto : FullAuditedEntityDto<int>
{
    public int? PortalId      { get; set; }
    public int  CatalogueId   { get; set; }
    public string? RibbonText { get; set; }
    public string? Name       { get; set; }
    public string? Slug       { get; set; }
    public string? Summary    { get; set; }
    public string? Sku        { get; set; }
    public double  Price      { get; set; }
    public double? DiscountPrice { get; set; }
    public string? Weight     { get; set; }
    public int?    VendorId   { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public int? Category1Id   { get; set; }
    public int? Category2Id   { get; set; }
    public int? Category3Id   { get; set; }
    public bool    Bundle     { get; set; }
    public string? BundleCode { get; set; }
    public ProductStatus Status      { get; set; }
    public bool    IsActive          { get; set; }
    public bool    IsFeatured        { get; set; }
    public int     DisplayOrder      { get; set; }
    public int     StockQuantity     { get; set; }
    public string? PrimaryImage      { get; set; }
    public string? Description       { get; set; }
    public string? MetaTitle         { get; set; }
    public string? MetaDescription   { get; set; }
    public string? MetaKeywords      { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
}
