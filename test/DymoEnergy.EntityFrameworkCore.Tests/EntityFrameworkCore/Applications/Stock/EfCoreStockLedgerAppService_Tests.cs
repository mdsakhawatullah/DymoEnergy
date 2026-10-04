using DymoEnergy.Stock;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Stock;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreStockLedgerAppService_Tests : StockLedgerAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
