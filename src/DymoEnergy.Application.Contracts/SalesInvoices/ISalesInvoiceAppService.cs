using System.Collections.Generic;
using System.Threading.Tasks;
using DymoEnergy.Shared;
using Volo.Abp.Application.Services;

namespace DymoEnergy.SalesInvoices;

public interface ISalesInvoiceAppService : IApplicationService
{
    Task<SalesInvoiceDto> GetAsync(int id);
    Task<DymoPagedResultDto<SalesInvoiceDto>> GetListDataAsync(SalesInvoiceFilterDto input);
    Task<SalesInvoiceSummaryDto> GetSummaryAsync(SalesInvoiceSummaryInputDto input);
    Task<SalesInvoiceDto> CreateInvoiceDataAsync(CreateUpdateSalesInvoiceDto input);
    Task<SalesInvoiceDto> UpdateAsync(int id, CreateUpdateSalesInvoiceDto input);
    Task DeleteAsync(int id);
    Task<List<SalesInvoiceItemDto>> GetSaleInvoiceItemAsync(int invoiceId);

    // ── Payments ─────────────────────────────────────────────────────────────
    Task<List<SalesInvoicePaymentDto>> GetPaymentsAsync(int id);
    Task<SalesInvoiceDto> CollectPaymentAsync(int id, CollectSalesInvoicePaymentDto input);
    Task<SalesInvoiceDto> DeletePaymentAsync(int id, int paymentId);
}
