using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.UserSiteSettings;

public class CreateUpdateUserSiteSettingDto
{
    public int? PortalId { get; set; }

    // ── General ───────────────────────────────────────────────────────────
    [MaxLength(1024)]
    public string? BackgroundImage { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    // ── Brand colours ─────────────────────────────────────────────────────
    [MaxLength(32)]
    public string? ButtonColor { get; set; }

    [MaxLength(32)]
    public string? PrimaryColor { get; set; }

    [MaxLength(32)]
    public string? BodyColor { get; set; }

    // ── Background colours ────────────────────────────────────────────────
    [MaxLength(32)]
    public string? BackgroundColor { get; set; }

    [MaxLength(32)]
    public string? CardBgColor { get; set; }

    // ── Navbar ────────────────────────────────────────────────────────────
    [MaxLength(32)]
    public string? NavbarBgColor { get; set; }

    [MaxLength(32)]
    public string? NavbarTextColor { get; set; }

    // ── Sidebar ───────────────────────────────────────────────────────────
    [MaxLength(32)]
    public string? SidebarBgColor { get; set; }

    [MaxLength(32)]
    public string? SidebarTextColor { get; set; }

    [MaxLength(32)]
    public string? SidebarActiveBgColor { get; set; }

    // ── Buttons ───────────────────────────────────────────────────────────
    [MaxLength(32)]
    public string? ButtonPrimaryBgColor { get; set; }

    [MaxLength(32)]
    public string? ButtonPrimaryTextColor { get; set; }

    // ── Typography ────────────────────────────────────────────────────────
    [MaxLength(128)]
    public string? FontFamily { get; set; }

    [MaxLength(16)]
    public string? FontSizeBase { get; set; }

    // ── About Section ─────────────────────────────────────────────────────
    [MaxLength(256)]
    public string? AboutTitle { get; set; }

    [MaxLength(4000)]
    public string? AboutDescription { get; set; }

    // ── Catalogues Section ────────────────────────────────────────────────
    [MaxLength(256)]
    public string? CataloguesTitle { get; set; }

    [MaxLength(1000)]
    public string? CataloguesDescription { get; set; }

    // ── Social Media ──────────────────────────────────────────────────────
    [MaxLength(512)] public string? SocialLinkedinUrl  { get; set; }
    [MaxLength(512)] public string? SocialInstagramUrl { get; set; }
    [MaxLength(512)] public string? SocialFacebookUrl  { get; set; }
    [MaxLength(512)] public string? SocialTwitterUrl   { get; set; }
    [MaxLength(512)] public string? SocialYoutubeUrl   { get; set; }

    public bool IsActive { get; set; }

    // ── Images ────────────────────────────────────────────────────────────
    public List<CreateUpdateUserSiteSettingImageDto> Images { get; set; } = new();
}
