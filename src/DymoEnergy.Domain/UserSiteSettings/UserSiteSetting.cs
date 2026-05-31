using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.UserSiteSettings;

public class UserSiteSetting : FullAuditedAggregateRoot<int>
{
    public int? PortalId { get; set; }

    // ── General ───────────────────────────────────────────────────────────
    public string? BackgroundImage { get; set; }
    public string? Description     { get; set; }

    // ── Brand colours ─────────────────────────────────────────────────────
    public string? ButtonColor    { get; set; }
    public string? PrimaryColor   { get; set; }
    public string? BodyColor      { get; set; }

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
    public string? FontFamily  { get; set; }
    public string? FontSizeBase { get; set; }
    public bool IsActive { get; set; }
}
