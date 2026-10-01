using DymoEnergy.Analytics;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Analytics;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreAnalyticsAppService_Tests : AnalyticsAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
