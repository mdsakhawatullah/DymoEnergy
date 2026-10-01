using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Analytics;

/// <summary>Read-only aggregates behind Analytics &amp; Reporting. Everything is computed from
/// sales invoices, payments, web orders, products and quote requests — nothing is stored.</summary>
public interface IAnalyticsAppService : IApplicationService
{
    /// <summary>Channels for the "All showrooms &amp; online" picker.</summary>
    Task<List<string>> GetChannelsAsync();

    Task<AnalyticsOverviewDto>  GetOverviewAsync(AnalyticsFilterDto input);
    Task<AnalyticsSalesDto>     GetSalesAsync(AnalyticsFilterDto input);
    Task<AnalyticsProductsDto>  GetProductsAsync(AnalyticsFilterDto input);
    Task<AnalyticsMoneyDto>     GetMoneyAsync(AnalyticsFilterDto input);
    Task<AnalyticsCustomersDto> GetCustomersAsync(AnalyticsFilterDto input);
    Task<AnalyticsReportDto>    GetReportAsync(AnalyticsReportInputDto input);

    Task SetSalesTargetAsync(SetSalesTargetDto input);
}
