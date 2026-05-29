using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.AdminSiteSettings;

public class CreateUpdateAdminSiteSettingDto
{
    [MaxLength(128)]
    public string SiteName { get; set; }

    [MaxLength(32)]
    public string ButtonColor { get; set; }

    [MaxLength(256)]
    public string Tagline { get; set; }

    [MaxLength(512)]
    public string LogoUrl { get; set; }

    // ── Brand colours ────────────────────────────────────────
    [MaxLength(32)]
    public string PrimaryColor { get; set; }

    [MaxLength(32)]
    public string SecondaryColor { get; set; }

    [MaxLength(32)]
    public string AccentColor { get; set; }

    // ── Text colours ─────────────────────────────────────────
    [MaxLength(32)]
    public string TextColor { get; set; }

    [MaxLength(32)]
    public string TextMutedColor { get; set; }

    [MaxLength(32)]
    public string LinkColor { get; set; }

    // ── Background colours ───────────────────────────────────
    [MaxLength(32)]
    public string BackgroundColor { get; set; }

    [MaxLength(32)]
    public string CardBgColor { get; set; }

    // ── Navbar ───────────────────────────────────────────────
    [MaxLength(32)]
    public string NavbarBgColor { get; set; }

    [MaxLength(32)]
    public string NavbarTextColor { get; set; }

    // ── Sidebar ──────────────────────────────────────────────
    [MaxLength(32)]
    public string SidebarBgColor { get; set; }

    [MaxLength(32)]
    public string SidebarTextColor { get; set; }

    [MaxLength(32)]
    public string SidebarActiveBgColor { get; set; }

    // ── Buttons ──────────────────────────────────────────────
    [MaxLength(32)]
    public string ButtonPrimaryBgColor { get; set; }

    [MaxLength(32)]
    public string ButtonPrimaryTextColor { get; set; }

    // ── Typography ───────────────────────────────────────────
    [MaxLength(128)]
    public string FontFamily { get; set; }

    [MaxLength(16)]
    public string FontSizeBase { get; set; }

    public bool IsActive { get; set; }

    // ── Location ──────────────────────────────────────────────────────────
    [MaxLength(256)]
    public string Address { get; set; }

    [MaxLength(100)]
    public string City { get; set; }

    [MaxLength(100)]
    public string State { get; set; }

    [MaxLength(20)]
    public string ZipCode { get; set; }

    [MaxLength(100)]
    public string Country { get; set; }

    // ── Contact ───────────────────────────────────────────────────────────
    [MaxLength(32)]
    public string Phone { get; set; }

    [MaxLength(256)]
    public string Email { get; set; }

    [MaxLength(256)]
    public string SupportEmail { get; set; }

    [MaxLength(32)]
    public string WhatsApp { get; set; }
}
