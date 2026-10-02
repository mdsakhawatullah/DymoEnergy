using DymoEnergy.Compliance;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Compliance;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreComplianceAppService_Tests : ComplianceAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
