using Volo.Abp.Application.Dtos;

namespace DymoEnergy.AdminSiteSettings;

public class AdminSiteSettingDto : AuditedEntityDto<int>
{
    public string SiteName { get; set; }
    public string ButtonColor { get; set; }
    public string Tagline { get; set; }
    public string LogoUrl { get; set; }

    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string AccentColor { get; set; }

    public string TextColor { get; set; }
    public string TextMutedColor { get; set; }
    public string LinkColor { get; set; }

    public string BackgroundColor { get; set; }
    public string CardBgColor { get; set; }

    public string NavbarBgColor { get; set; }
    public string NavbarTextColor { get; set; }

    public string SidebarBgColor { get; set; }
    public string SidebarTextColor { get; set; }
    public string SidebarActiveBgColor { get; set; }

    public string ButtonPrimaryBgColor { get; set; }
    public string ButtonPrimaryTextColor { get; set; }

    public string FontFamily { get; set; }
    public string FontSizeBase { get; set; }

    public bool IsActive { get; set; }
}
