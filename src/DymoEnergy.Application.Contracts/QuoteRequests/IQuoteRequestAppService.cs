using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.QuoteRequests;

public interface IQuoteRequestAppService : IApplicationService
{
    Task CreateAsync(CreateQuoteRequestDto input);
    Task<QuoteRequestDto> GetAsync(int id);
    Task<DymoPagedResultDto<QuoteRequestDto>> GetListDataAsync(QuoteRequestFilterDto input);
    Task<QuoteRequestSummaryDto> GetSummaryAsync(QuoteRequestFilterDto input);
    Task<QuoteRequestDto> UpdateStatusAsync(int id, UpdateQuoteRequestStatusDto input);
    Task<QuoteRequestDto> UpdateDetailsAsync(int id, UpdateQuoteRequestDetailsDto input);
    Task DeleteAsync(int id);
}
