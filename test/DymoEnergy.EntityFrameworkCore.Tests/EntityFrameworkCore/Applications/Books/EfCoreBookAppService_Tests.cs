using DymoEnergy.Books;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.Books;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreBookAppService_Tests : BookAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{

}