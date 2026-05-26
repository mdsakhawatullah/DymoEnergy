using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Data;

/* This is used if database provider does't define
 * IDymoEnergyDbSchemaMigrator implementation.
 */
public class NullDymoEnergyDbSchemaMigrator : IDymoEnergyDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
