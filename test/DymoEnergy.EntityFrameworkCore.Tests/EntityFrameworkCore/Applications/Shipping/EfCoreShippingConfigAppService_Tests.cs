using DymoEnergy.Shipping;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Shipping;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreShippingConfigAppService_Tests : ShippingConfigAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
