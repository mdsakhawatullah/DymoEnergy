using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Stock;

public interface IStockEntryAppService : IApplicationService
{
    Task<StockOverviewDto>    GetOverviewAsync();
    Task<StockEntriesPageDto> GetEntriesAsync(GetStockEntriesInput input);
    Task<StockEntryDto>       GetAsync(int id);
    /// <summary>The number the next new entry will get (shown on the new-entry page).</summary>
    Task<string>              GetNextNumberAsync();

    Task<StockEntryDto> CreateAsync(SaveStockEntryDto input);
    /// <summary>Drafts only.</summary>
    Task<StockEntryDto> UpdateAsync(int id, SaveStockEntryDto input);
    /// <summary>Drafts only.</summary>
    Task DeleteAsync(int id);
    /// <summary>Applies a draft to stock. After this it cannot be changed, only reversed.</summary>
    Task<StockEntryDto> PostAsync(int id);
    /// <summary>Undoes a posted entry with a new, opposite entry.</summary>
    Task<StockEntryDto> ReverseAsync(int id);

    Task<StockAttachmentDto> AddAttachmentAsync(int id, AddStockAttachmentDto input);
    Task DeleteAttachmentAsync(int attachmentId);

    Task<List<StockProductDto>> GetProductsAsync(GetStockProductsInput input);

    Task<WarehouseDto> CreateWarehouseAsync(CreateUpdateWarehouseDto input);
    Task<WarehouseDto> UpdateWarehouseAsync(int id, CreateUpdateWarehouseDto input);
    Task<StockSupplierDto> CreateSupplierAsync(CreateUpdateStockSupplierDto input);
    Task<StockSupplierDto> UpdateSupplierAsync(int id, CreateUpdateStockSupplierDto input);
}
