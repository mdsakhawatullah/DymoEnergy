using Xunit;

namespace DymoEnergy.EntityFrameworkCore;

[CollectionDefinition(DymoEnergyTestConsts.CollectionDefinitionName)]
public class DymoEnergyEntityFrameworkCoreCollection : ICollectionFixture<DymoEnergyEntityFrameworkCoreFixture>
{

}
