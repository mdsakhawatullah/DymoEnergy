using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace DymoEnergy.AdminSiteSettings;

public interface IAdminSiteSettingAppService :
    ICrudAppService<
        AdminSiteSettingDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateAdminSiteSettingDto>
{
    /// <summary>Returns the currently active site setting, or null if none is set.</summary>
    Task<AdminSiteSettingDto> GetActiveAsync();

    /// <summary>Marks the given setting as active and deactivates all others.</summary>
    Task<AdminSiteSettingDto> SetActiveAsync(Guid id);
}
