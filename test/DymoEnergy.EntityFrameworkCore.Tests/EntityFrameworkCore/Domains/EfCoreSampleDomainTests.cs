using DymoEnergy.Samples;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Domains;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<DymoEnergyEntityFrameworkCoreTestModule>
{

}
