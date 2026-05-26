using Volo.Abp.Modularity;

namespace DymoEnergy;

[DependsOn(
    typeof(DymoEnergyApplicationModule),
    typeof(DymoEnergyDomainTestModule)
)]
public class DymoEnergyApplicationTestModule : AbpModule
{

}
