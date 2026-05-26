using DymoEnergy.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace DymoEnergy.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(DymoEnergyEntityFrameworkCoreModule),
    typeof(DymoEnergyApplicationContractsModule)
)]
public class DymoEnergyDbMigratorModule : AbpModule
{
}
