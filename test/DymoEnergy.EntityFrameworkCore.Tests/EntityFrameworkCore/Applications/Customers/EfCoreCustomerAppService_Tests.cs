using DymoEnergy.Customers;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Customers;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreCustomerAppService_Tests : CustomerAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
