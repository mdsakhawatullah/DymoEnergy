using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace DymoEnergy.UserSiteSettings;

public interface IUserSiteSettingAppService : IApplicationService
{
    Task<UserSiteSettingDto> GetAsync(int id);
    Task<PagedResultDto<UserSiteSettingDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<UserSiteSettingDto> CreateAsync(CreateUpdateUserSiteSettingDto input);
    Task<UserSiteSettingDto> UpdateAsync(int id, CreateUpdateUserSiteSettingDto input);
    Task DeleteAsync(int id);

    Task<UserSiteSettingDto> GetActiveAsync();
    Task<UserSiteSettingDto> SetActiveAsync(int id);
}
