using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.QuoteRequests;

public interface IQuoteRequestAppService : IApplicationService
{
    Task CreateAsync(CreateQuoteRequestDto input);
    Task<DymoPagedResultDto<QuoteRequestDto>> GetListDataAsync(QuoteRequestFilterDto input);
    Task<QuoteRequestDto> UpdateStatusAsync(int id, UpdateQuoteRequestStatusDto input);
    Task DeleteAsync(int id);
}
