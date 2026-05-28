using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace DymoEnergy.Catalogues;

/// <summary>
/// Child entity — one image belonging to a <see cref="Catalogue"/>.
/// An aggregate root may own many images of different <see cref="CatalogueImageType"/>s.
/// </summary>
public class CatalogueImage : Entity<int>
{
    // ── Foreign key ──────────────────────────────────────────────────────
    public int CatalogueId { get; set; }

    // ── Image data ───────────────────────────────────────────────────────
    public string? ImageUrl    { get; set; } = null!;
    public CatalogueImageType ImageType    { get; set; }
    public string? Title      { get; set; }
    public string? AltText    { get; set; }
    public int     DisplayOrder { get; set; }
    public bool    IsActive    { get; set; }
}
