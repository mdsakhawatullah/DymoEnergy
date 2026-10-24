using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace DymoEnergy.BrandPortfolios;

public interface IBrandPortfolioAppService : IApplicationService
{
    Task<BrandPortfolioDto> GetAsync(int id);
    Task<PagedResultDto<BrandPortfolioListDto>> GetListAsync(GetBrandPortfolioListDto input);
    Task<BrandPortfolioDto> CreateAsync(CreateUpdateBrandPortfolioDto input);
    Task<BrandPortfolioDto> UpdateAsync(int id, CreateUpdateBrandPortfolioDto input);
    Task DeleteAsync(int id);

    /// <summary>The storefront page, looked up by its web address. Published pages only.</summary>
    Task<BrandPortfolioDto> GetBySlugAsync(string slug);

    /// <summary>What the storefront brands index shows — published pages only.</summary>
    Task<ListResultDto<BrandPortfolioListDto>> GetPublishedAsync();

    /// <summary>Flips the "page is public" switch from the list or the editor header.</summary>
    Task<BrandPortfolioDto> SetPublishedAsync(int id, bool isPublished);
}
