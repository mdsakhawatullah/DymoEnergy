using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.UserSiteSettings;

public class CreateUpdateUserSiteSettingImageDto
{
    public int? Id { get; set; }

    [MaxLength(1024)]
    public string? ImageUrl { get; set; }

    [MaxLength(256)]
    public string? Title { get; set; }

    [MaxLength(256)]
    public string? AltText { get; set; }

    public int  DisplayOrder { get; set; }
    public bool IsActive     { get; set; }
}
