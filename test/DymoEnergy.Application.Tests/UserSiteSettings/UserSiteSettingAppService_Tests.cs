using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.UserSiteSettings;

public abstract class UserSiteSettingAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    [Fact]
    public async Task Should_List_And_Get_The_Setting_The_Admin_Page_Loads()
    {
        var service = GetRequiredService<IUserSiteSettingAppService>();
        await service.CreateAsync(new CreateUpdateUserSiteSettingDto { PrimaryColor = "#0E6B3F", IsActive = true });

        var list = await service.GetListAsync(new PagedAndSortedResultRequestDto { SkipCount = 0, MaxResultCount = 1 });
        list.TotalCount.ShouldBe(1);
        var full = await service.GetAsync(list.Items[0].Id);
        full.PrimaryColor.ShouldBe("#0E6B3F");
    }
}
