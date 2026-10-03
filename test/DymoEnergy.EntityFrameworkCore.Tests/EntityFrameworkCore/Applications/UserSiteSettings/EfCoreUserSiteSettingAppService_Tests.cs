using DymoEnergy.UserSiteSettings;
using Xunit;

namespace DymoEnergy.EntityFrameworkCore.Applications.UserSiteSettings;

[Collection(DymoEnergyTestConsts.CollectionDefinitionName)]
public class EfCoreUserSiteSettingAppService_Tests : UserSiteSettingAppService_Tests<DymoEnergyEntityFrameworkCoreTestModule>
{
}
