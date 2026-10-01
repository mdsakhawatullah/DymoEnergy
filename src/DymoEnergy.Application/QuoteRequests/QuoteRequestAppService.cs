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
            Name          = input.Name.Trim(),
            Phone         = Clean(input.Phone),
            Email         = Clean(input.Email),
            Interest      = Clean(input.Interest),
            Message       = Clean(input.Message),
            EstimatedSize = Clean(input.EstimatedSize),
            MonthlyBill   = Clean(input.MonthlyBill),
            RoofSite      = Clean(input.RoofSite),
            Location      = Clean(input.Location),
            Status        = QuoteRequestStatus.New,
        };

        await _quoteRequestRepository.InsertAsync(quoteRequest, autoSave: true);
    }

    public async Task<QuoteRequestDto> GetAsync(int id)
    {
        return MapToDto(await _quoteRequestRepository.GetAsync(id));
    }

    public async Task<DymoPagedResultDto<QuoteRequestDto>> GetListDataAsync(QuoteRequestFilterDto input)
    {
        var query = ApplyFilters(await _quoteRequestRepository.GetQueryableAsync(), input.Filter, input.Interest);

        if (input.Status.HasValue)
            query = query.Where(q => q.Status == input.Status);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderByDescending(q => q.CreationTime)
            .ThenByDescending(q => q.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(query);
        return new DymoPagedResultDto<QuoteRequestDto>(totalCount, items.Select(MapToDto).ToList());
    }

    /// <summary>Tab counts for the current search / system-type filter (status is ignored).</summary>
    public async Task<QuoteRequestSummaryDto> GetSummaryAsync(QuoteRequestFilterDto input)
    {
        var all   = await _quoteRequestRepository.GetQueryableAsync();
        var query = ApplyFilters(all, input.Filter, input.Interest);

        var byStatus = await AsyncExecuter.ToListAsync(
            query.GroupBy(q => q.Status).Select(g => new { Status = g.Key, Count = g.Count() }));
        int Count(QuoteRequestStatus s) => byStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        // Dropdown options come from every request, not just the filtered set
        var types = await AsyncExecuter.ToListAsync(
            all.Where(q => q.Interest != null && q.Interest != "")
               .Select(q => q.Interest!)
               .Distinct()
               .OrderBy(t => t));

        return new QuoteRequestSummaryDto
        {
            AllCount       = byStatus.Sum(x => x.Count),
            NewCount       = Count(QuoteRequestStatus.New),
            ContactedCount = Count(QuoteRequestStatus.Contacted),
            QuotedCount    = Count(QuoteRequestStatus.Quoted),
            ClosedCount    = Count(QuoteRequestStatus.Closed),
            SystemTypes    = types,
        };
    }

    [Authorize(DymoEnergyPermissions.QuoteRequests.Edit)]
    public async Task<QuoteRequestDto> UpdateStatusAsync(int id, UpdateQuoteRequestStatusDto input)
    {
        var quoteRequest = await _quoteRequestRepository.GetAsync(id);
        quoteRequest.Status = input.Status;
        await _quoteRequestRepository.UpdateAsync(quoteRequest, autoSave: true);
        return MapToDto(quoteRequest);
    }

    [Authorize(DymoEnergyPermissions.QuoteRequests.Edit)]
    public async Task<QuoteRequestDto> UpdateDetailsAsync(int id, UpdateQuoteRequestDetailsDto input)
    {
        var q = await _quoteRequestRepository.GetAsync(id);
        q.Interest      = Clean(input.Interest);
        q.EstimatedSize = Clean(input.EstimatedSize);
        q.MonthlyBill   = Clean(input.MonthlyBill);
        q.RoofSite      = Clean(input.RoofSite);
        q.Location      = Clean(input.Location);
        q.AdminNote     = Clean(input.AdminNote);
        await _quoteRequestRepository.UpdateAsync(q, autoSave: true);
        return MapToDto(q);
    }

    [Authorize(DymoEnergyPermissions.QuoteRequests.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _quoteRequestRepository.DeleteAsync(id, autoSave: true);
    }

    private static IQueryable<QuoteRequest> ApplyFilters(IQueryable<QuoteRequest> query, string? filter, string? interest)
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.Trim();
            query = query.Where(q =>
                q.Name.Contains(f) ||
                (q.Phone    != null && q.Phone.Contains(f))    ||
                (q.Email    != null && q.Email.Contains(f))    ||
                (q.Interest != null && q.Interest.Contains(f)) ||
                (q.Location != null && q.Location.Contains(f)) ||
                (q.Message  != null && q.Message.Contains(f)));
        }

        if (!string.IsNullOrWhiteSpace(interest))
            query = query.Where(q => q.Interest == interest);

        return query;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static QuoteRequestDto MapToDto(QuoteRequest q) => new()
    {
        Id                   = q.Id,
        Name                 = q.Name,
        Phone                = q.Phone,
        Email                = q.Email,
        Interest             = q.Interest,
        Message              = q.Message,
        Status               = q.Status,
        EstimatedSize        = q.EstimatedSize,
        MonthlyBill          = q.MonthlyBill,
        RoofSite             = q.RoofSite,
        Location             = q.Location,
        AdminNote            = q.AdminNote,
        CreationTime         = q.CreationTime,
        LastModificationTime = q.LastModificationTime,
    };
}
