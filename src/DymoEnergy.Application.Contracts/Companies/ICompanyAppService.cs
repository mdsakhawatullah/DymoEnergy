using System.Collections.Generic;
using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Companies;

public interface ICompanyAppService : IApplicationService
{
    Task<CompanyDto>                     GetAsync(int id);
    Task<DymoPagedResultDto<CompanyDto>> GetListDataAsync(CompanyFilterDto input);
    Task<List<SelectListDto>>            GetSelectListAsync();
    Task<CompanyDto>                     CreateCompanyDataAsync(CreateUpdateCompanyDto input);
    Task<CompanyDto>                     UpdateAsync(int id, CreateUpdateCompanyDto input);
    Task                                 DeleteAsync(int id);
}
