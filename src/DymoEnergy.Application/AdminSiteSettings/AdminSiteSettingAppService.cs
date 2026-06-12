using System.Threading.Tasks;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.AdminSiteSettings;

[Authorize(DymoEnergyPermissions.AdminSiteSettings.Default)]
public class AdminSiteSettingAppService : ApplicationService, IAdminSiteSettingAppService
{
    private readonly IRepository<AdminSiteSetting, int> _repository;

    public AdminSiteSettingAppService(IRepository<AdminSiteSetting, int> repository)
    {
        _repository = repository;
    }

    public async Task<AdminSiteSettingDto> GetAsync(int id)
    {
        var entity = await _repository.GetAsync(id);
        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(entity);
    }

    public async Task<PagedResultDto<AdminSiteSettingDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var totalCount = await _repository.GetCountAsync();
        var items = await _repository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting ?? nameof(AdminSiteSetting.Id));
        return new PagedResultDto<AdminSiteSettingDto>(
            totalCount,
            ObjectMapper.Map<System.Collections.Generic.List<AdminSiteSetting>, System.Collections.Generic.List<AdminSiteSettingDto>>(items)
        );
    }

    [Authorize(DymoEnergyPermissions.AdminSiteSettings.Create)]
    public async Task<AdminSiteSettingDto> CreateAsync(CreateUpdateAdminSiteSettingDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateAdminSiteSettingDto, AdminSiteSetting>(input);
        await _repository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(entity);
    }

    [Authorize(DymoEnergyPermissions.AdminSiteSettings.Edit)]
    public async Task<AdminSiteSettingDto> UpdateAsync(int id, CreateUpdateAdminSiteSettingDto input)
    {
        var entity = await _repository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await _repository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(entity);
    }

    [Authorize(DymoEnergyPermissions.AdminSiteSettings.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id, autoSave: true);
    }

    [AllowAnonymous]
    public async Task<AdminSiteSettingDto> GetActiveAsync()
    {
        var entity = await _repository.FindAsync(x => x.IsActive);
        if (entity == null) return null;
        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(entity);
    }

    [Authorize(DymoEnergyPermissions.AdminSiteSettings.Edit)]
    public async Task<AdminSiteSettingDto> SetActiveAsync(int id)
    {
        var allSettings = await _repository.GetListAsync();
        foreach (var s in allSettings)
        {
            if (s.IsActive)
            {
                s.IsActive = false;
                await _repository.UpdateAsync(s);
            }
        }

        var target = await _repository.GetAsync(id);
        target.IsActive = true;
        await _repository.UpdateAsync(target, autoSave: true);

        return ObjectMapper.Map<AdminSiteSetting, AdminSiteSettingDto>(target);
    }
}
