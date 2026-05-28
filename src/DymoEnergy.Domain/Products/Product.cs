using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Products;

public class Product : FullAuditedAggregateRoot<int>
{
    public int? PortalId { get; set; }
    public int CatalogueId { get; set; }
    public string? RibbonText { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Sku { get; set; }
    public double Price { get; set; }
    public double? DiscountPrice { get; set; }
    public string? Weight { get; set; }
    public int? VendorId { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorName { get; set; }
    public int? Category1Id { get; set; }
    public int? Category2Id { get; set; }
    public int? Category3Id { get; set; }
    public bool Bundle     { get; set; }
    public string? BundleCode { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public int StockQuantity { get; set; }
    public string?  PrimaryImage { get; set; } 
    public string?  Description  { get; set; }

    // ── SEO ───────────────────────────────────────────────────────────────
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    // ── Child collections ─────────────────────────────────────────────────
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}
