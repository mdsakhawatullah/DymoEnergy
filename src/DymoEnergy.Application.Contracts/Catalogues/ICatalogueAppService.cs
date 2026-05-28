using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Catalogues;

public interface ICatalogueAppService : IApplicationService
{
    Task<CatalogueDto>GetAsync(int id);
    Task<CatalogueDto>GetBySlugAsync(string slug);
    Task<DymoPagedResultDto<CatalogueDto>>GetListDataAsync(CatalogueFilterDto input);
    Task<CatalogueDto>CreateCatalogueDataAsync(CreateUpdateCatalogueDto input);
    Task<CatalogueDto>UpdateAsync(int id, CreateUpdateCatalogueDto input);
    Task DeleteAsync(int id);
}
