using Volo.Abp.Modularity;

namespace DymoEnergy;

[DependsOn(
    typeof(DymoEnergyDomainModule),
    typeof(DymoEnergyTestBaseModule)
)]
public class DymoEnergyDomainTestModule : AbpModule
{

}
