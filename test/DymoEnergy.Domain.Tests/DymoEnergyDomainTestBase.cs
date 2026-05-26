using Volo.Abp.Modularity;

namespace DymoEnergy;

/* Inherit from this class for your domain layer tests. */
public abstract class DymoEnergyDomainTestBase<TStartupModule> : DymoEnergyTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
