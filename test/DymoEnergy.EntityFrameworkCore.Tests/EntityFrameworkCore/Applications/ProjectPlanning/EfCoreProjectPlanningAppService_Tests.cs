using DymoEnergy.ProjectPlanning;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.ProjectPlanning;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreProjectPlanningAppService_Tests : ProjectPlanningAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
