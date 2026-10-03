using DymoEnergy.Finance;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Finance;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreFinanceAppService_Tests : FinanceAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
