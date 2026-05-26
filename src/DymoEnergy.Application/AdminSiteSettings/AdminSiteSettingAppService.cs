using System;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.AdminSiteSettings;

[Authorize(DymoEnergyPermissions.AdminSiteSettings.Default)]
public class AdminSiteSettingAppService :
    CrudAppService<
        AdminSiteSetting,
        AdminSiteSettingDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateAdminSiteSettingDto>,
    IAdminSiteSettingAppService
{
    public AdminSiteSettingAppService(IRepository<AdminSiteSetting, Guid> repository)
        : base(repository)
    {
        GetPolicyName    = DymoEnergyPermissions.AdminSiteSettings.Default;
        GetListPolicyName = DymoEnergyPermissions.AdminSiteSettings.Default;
        CreatePolicyName  = DymoEnergyPermissions.AdminSiteSettings.Create;
        UpdatePolicyName  = DymoEnergyPermissions.AdminSiteSettings.Edit;
        DeletePolicyName  = DymoEnergyPermissions.AdminSiteSettings.Delete;
    }

    /// <inheritdoc/>
    public async Task<AdminSiteSettingDto> GetActiveAsync()
    {
        var setting = await Repository.FindAsync(x => x.IsActive);
        if (setting == null) return null;
        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(setting);
    }

    /// <inheritdoc/>
    [Authorize(DymoEnergyPermissions.AdminSiteSettings.Edit)]
    public async Task<AdminSiteSettingDto> SetActiveAsync(Guid id)
    {
        // Deactivate all other settings
        var allSettings = await Repository.GetListAsync();
        foreach (var s in allSettings)
        {
            if (s.IsActive)
            {
                s.IsActive = false;
                await Repository.UpdateAsync(s);
            }
        }

        // Activate the requested one
        var target = await Repository.GetAsync(id);
        target.IsActive = true;
        await Repository.UpdateAsync(target);

        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(target);
    }
}
