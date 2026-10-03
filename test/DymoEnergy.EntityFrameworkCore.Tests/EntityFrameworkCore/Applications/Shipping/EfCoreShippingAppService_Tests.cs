using DymoEnergy.Shipping;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Shipping;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreShippingAppService_Tests : ShippingAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
