using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DymoEnergy.Data;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.EntityFrameworkCore;

public class EntityFrameworkCoreDymoEnergyDbSchemaMigrator
    : IDymoEnergyDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreDymoEnergyDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the DymoEnergyDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<DymoEnergyDbContext>()
            .Database
            .MigrateAsync();
    }
}
