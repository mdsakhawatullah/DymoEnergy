using System.Collections.Generic;
using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Categories;

public interface ICategoryAppService : IApplicationService
{
    Task<CategoryDto>GetAsync(int id);
    Task<CategoryDto>GetBySlugAsync(string slug);
    Task<DymoPagedResultDto<CategoryDto>>GetListDataAsync(CategoryFilterDto input);
    Task<IEnumerable<SelectListDto>>GetSelectListAsync();
    Task<List<HomeCategoryShowcaseDto>>GetHomeShowcaseAsync();
    Task<CategoryDto>CreateCategoryDataAsync(CreateUpdateCategoryDto input);
    Task<CategoryDto>UpdateAsync(int id, CreateUpdateCategoryDto input);
    Task DeleteAsync(int id);
}
