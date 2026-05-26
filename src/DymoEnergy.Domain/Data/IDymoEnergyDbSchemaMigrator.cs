using System.Threading.Tasks;

namespace DymoEnergy.Data;

public interface IDymoEnergyDbSchemaMigrator
{
    Task MigrateAsync();
}
