using Volo.Abp.Modularity;

namespace DymoEnergy;

public abstract class DymoEnergyApplicationTestBase<TStartupModule> : DymoEnergyTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
