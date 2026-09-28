using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace DymoEnergy.Categories;

/// <summary>
/// Child entity — one image belonging to a <see cref="Category"/>.
/// An aggregate root may own many images of different <see cref="CategoryImageType"/>s.
/// </summary>
public class CategoryImage : Entity<int>
{
    // ── Foreign key ──────────────────────────────────────────────────────
    public int CategoryId { get; set; }

    // ── Image data ───────────────────────────────────────────────────────
    public string? ImageUrl    { get; set; } = null!;
    public CategoryImageType ImageType    { get; set; }
    public string? Title      { get; set; }
    public string? AltText    { get; set; }
    public int     DisplayOrder { get; set; }
    public bool    IsActive    { get; set; }
}
