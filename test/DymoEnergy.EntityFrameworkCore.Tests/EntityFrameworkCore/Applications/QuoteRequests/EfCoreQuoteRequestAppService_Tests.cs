using DymoEnergy.QuoteRequests;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.QuoteRequests;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreQuoteRequestAppService_Tests : QuoteRequestAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
