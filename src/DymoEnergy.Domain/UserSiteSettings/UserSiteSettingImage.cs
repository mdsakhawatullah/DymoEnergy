using Volo.Abp.Domain.Entities;

namespace DymoEnergy.UserSiteSettings;

public class UserSiteSettingImage : Entity<int>
{
    // ── Foreign key ───────────────────────────────────────────────────────
    public int UserSiteSettingId { get; set; }

    // ── Image data ────────────────────────────────────────────────────────
    public string? ImageUrl { get; set; }
    public string? Title { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
