using System;
using Volo.Abp.Domain.Entities;

namespace DymoEnergy.Products;

/// <summary>
/// Child entity — a customer review attached to a <see cref="Product"/>.
/// Reviews are moderated via <see cref="IsApproved"/> before appearing on the storefront.
/// </summary>
public class ProductReview : Entity<int>
{
    // ── Foreign key ──────────────────────────────────────────────────────
    public int ProductId { get; set; }

    // ── Reviewer identity ─────────────────────────────────────────────────
    public string? ReviewerName  { get; set; }
    public string? ReviewerEmail { get; set; }

    // ── Review content ────────────────────────────────────────────────────
    public int Rating { get; set; }   // 1 – 5
    public string? Title { get; set; }
    public string? Comment { get; set; }

    // ── Moderation ────────────────────────────────────────────────────────
    public bool IsApproved  { get; set; }
    public DateTime  ReviewDate  { get; set; }
}
