using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Customers;

public interface ICustomerAppService : IApplicationService
{
    Task<CustomerOverviewDto> GetOverviewAsync();
    Task<CustomersPageDto>    GetListAsync(GetCustomersInput input);
    Task<CustomerDto>         GetAsync(int id);

    Task<CustomerDto> CreateAsync(CreateUpdateCustomerDto input);
    Task<CustomerDto> UpdateAsync(int id, CreateUpdateCustomerDto input);
    Task DeleteAsync(int id);

    /// <summary>Attaches orders and invoices that carry this customer's phone but no customer yet.</summary>
    Task<LinkOrdersResultDto> LinkMatchingAsync(int id);

    /// <summary>Makes a customer out of each phone number in past orders that has none yet.</summary>
    Task<ImportCustomersResultDto> ImportFromOrdersAsync();
}
