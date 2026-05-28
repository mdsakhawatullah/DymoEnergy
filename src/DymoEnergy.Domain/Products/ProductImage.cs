using Volo.Abp.Domain.Entities;

namespace DymoEnergy.Products;

/// <summary>
/// Child entity — one gallery image belonging to a <see cref="Product"/>.
/// The primary/hero image is stored directly on <see cref="Product.PrimaryImage"/>;
/// this entity holds additional gallery images.
/// </summary>
public class ProductImage : Entity<int>
{
    // ── Foreign key ──────────────────────────────────────────────────────
    public int ProductId { get; set; }

    // ── Image data ───────────────────────────────────────────────────────
    public string? ImageUrl { get; set; }
    public string? Title { get; set; }
    public string? AltText { get; set; }
    public int     DisplayOrder { get; set; }
    public bool    IsActive { get; set; }
}
