using DymoEnergy.Samples;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<DymoEnergyEntityFrameworkCoreTestModule>
{

}
