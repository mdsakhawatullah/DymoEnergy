using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.UserSiteSettings;

public class UserSiteSettingDto : FullAuditedEntityDto<int>
{
    public int? PortalId { get; set; }

    // ── General ───────────────────────────────────────────────────────────
    public string? BackgroundImage { get; set; }
    public string? Description     { get; set; }

    // ── Brand colours ─────────────────────────────────────────────────────
    public string? ButtonColor  { get; set; }
    public string? PrimaryColor { get; set; }
    public string? BodyColor    { get; set; }

    // ── Background colours ────────────────────────────────────────────────
    public string? BackgroundColor { get; set; }
    public string? CardBgColor     { get; set; }

    // ── Navbar ────────────────────────────────────────────────────────────
    public string? NavbarBgColor   { get; set; }
    public string? NavbarTextColor { get; set; }

    // ── Sidebar ───────────────────────────────────────────────────────────
    public string? SidebarBgColor       { get; set; }
    public string? SidebarTextColor     { get; set; }
    public string? SidebarActiveBgColor { get; set; }

    // ── Buttons ───────────────────────────────────────────────────────────
    public string? ButtonPrimaryBgColor   { get; set; }
    public string? ButtonPrimaryTextColor { get; set; }

    // ── Typography ────────────────────────────────────────────────────────
    public string? FontFamily   { get; set; }
    public string? FontSizeBase { get; set; }

    // ── About Section ─────────────────────────────────────────────────────
    public string? AboutTitle       { get; set; }
    public string? AboutDescription { get; set; }

    // ── Catalogues Section ────────────────────────────────────────────────
    public string? CataloguesTitle       { get; set; }
    public string? CataloguesDescription { get; set; }

    // ── Social Media ──────────────────────────────────────────────────────
    public string? SocialLinkedinUrl  { get; set; }
    public string? SocialInstagramUrl { get; set; }
    public string? SocialFacebookUrl  { get; set; }
    public string? SocialTwitterUrl   { get; set; }
    public string? SocialYoutubeUrl   { get; set; }

    public bool IsActive { get; set; }

    // ── Images ────────────────────────────────────────────────────────────
    public List<UserSiteSettingImageDto> Images { get; set; } = new();
}
