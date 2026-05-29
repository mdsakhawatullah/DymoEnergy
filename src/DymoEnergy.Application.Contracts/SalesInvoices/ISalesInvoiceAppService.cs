using System.Collections.Generic;
using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.SalesInvoices;

public interface ISalesInvoiceAppService : IApplicationService
{
    Task<SalesInvoiceDto> GetAsync(int id);
    Task<DymoPagedResultDto<SalesInvoiceDto>> GetListDataAsync(SalesInvoiceFilterDto input);
    Task<SalesInvoiceDto> CreateInvoiceDataAsync(CreateUpdateSalesInvoiceDto input);
    Task<SalesInvoiceDto> UpdateAsync(int id, CreateUpdateSalesInvoiceDto input);
    Task DeleteAsync(int id);
    Task<List<SalesInvoiceItemDto>> GetSaleInvoiceItemAsync(int invoiceId);
}
