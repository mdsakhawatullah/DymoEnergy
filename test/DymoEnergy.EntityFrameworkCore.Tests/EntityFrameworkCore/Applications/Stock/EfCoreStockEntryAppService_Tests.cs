using DymoEnergy.Stock;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Stock;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreStockEntryAppService_Tests : StockEntryAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
