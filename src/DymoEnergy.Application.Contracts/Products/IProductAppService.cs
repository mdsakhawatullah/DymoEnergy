using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Products;

public interface IProductAppService : IApplicationService
{
    Task<ProductDto> GetAsync(int id);
    Task<ProductDto> GetBySlugAsync(string slug);
    Task<DymoPagedResultDto<ProductDto>> GetListDataAsync(ProductFilterDto input);
    Task<ProductDto> CreateProductDataAsync(CreateUpdateProductDto input);
    Task<ProductDto> UpdateAsync(int id, CreateUpdateProductDto input);
    Task DeleteAsync(int id);
}
