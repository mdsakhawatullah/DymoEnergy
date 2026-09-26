using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.QuoteRequests;

[Authorize(DymoEnergyPermissions.QuoteRequests.Default)]
public class QuoteRequestAppService : ApplicationService, IQuoteRequestAppService
{
    private readonly IRepository<QuoteRequest, int> _quoteRequestRepository;

    public QuoteRequestAppService(IRepository<QuoteRequest, int> quoteRequestRepository)
    {
        _quoteRequestRepository = quoteRequestRepository;
    }

    /// <summary>Public storefront endpoint — anyone can submit a quote request.</summary>
    [AllowAnonymous]
    public async Task CreateAsync(CreateQuoteRequestDto input)
    {
        var quoteRequest = new QuoteRequest
        {
            Name     = input.Name.Trim(),
            Phone    = input.Phone?.Trim(),
            Email    = input.Email?.Trim(),
            Interest = input.Interest?.Trim(),
            Message  = input.Message?.Trim(),
            Status   = QuoteRequestStatus.New,
        };

        await _quoteRequestRepository.InsertAsync(quoteRequest, autoSave: true);
    }

    public async Task<DymoPagedResultDto<QuoteRequestDto>> GetListDataAsync(QuoteRequestFilterDto input)
    {
        var query = await _quoteRequestRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(q =>
                q.Name.Contains(input.Filter) ||
                (q.Phone    != null && q.Phone.Contains(input.Filter))    ||
                (q.Email    != null && q.Email.Contains(input.Filter))    ||
                (q.Interest != null && q.Interest.Contains(input.Filter)) ||
                (q.Message  != null && q.Message.Contains(input.Filter)));

        if (input.Status.HasValue)
            query = query.Where(q => q.Status == input.Status);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderByDescending(q => q.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(query);
        return new DymoPagedResultDto<QuoteRequestDto>(totalCount, items.Select(MapToDto).ToList());
    }

    [Authorize(DymoEnergyPermissions.QuoteRequests.Edit)]
    public async Task<QuoteRequestDto> UpdateStatusAsync(int id, UpdateQuoteRequestStatusDto input)
    {
        var quoteRequest = await _quoteRequestRepository.GetAsync(id);
        quoteRequest.Status = input.Status;
        await _quoteRequestRepository.UpdateAsync(quoteRequest, autoSave: true);
        return MapToDto(quoteRequest);
    }

    [Authorize(DymoEnergyPermissions.QuoteRequests.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _quoteRequestRepository.DeleteAsync(id, autoSave: true);
    }

    private static QuoteRequestDto MapToDto(QuoteRequest q) => new()
    {
        Id           = q.Id,
        Name         = q.Name,
        Phone        = q.Phone,
        Email        = q.Email,
        Interest     = q.Interest,
        Message      = q.Message,
        Status       = q.Status,
        CreationTime = q.CreationTime,
    };
}
