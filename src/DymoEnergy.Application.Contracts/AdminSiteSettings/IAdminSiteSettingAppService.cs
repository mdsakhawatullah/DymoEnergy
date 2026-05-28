using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace DymoEnergy.AdminSiteSettings;

public interface IAdminSiteSettingAppService : IApplicationService
{
    Task<AdminSiteSettingDto> GetAsync(int id);
    Task<PagedResultDto<AdminSiteSettingDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<AdminSiteSettingDto> CreateAsync(CreateUpdateAdminSiteSettingDto input);
    Task<AdminSiteSettingDto> UpdateAsync(int id, CreateUpdateAdminSiteSettingDto input);
    Task DeleteAsync(int id);

    Task<AdminSiteSettingDto> GetActiveAsync();
    Task<AdminSiteSettingDto> SetActiveAsync(int id);
}
