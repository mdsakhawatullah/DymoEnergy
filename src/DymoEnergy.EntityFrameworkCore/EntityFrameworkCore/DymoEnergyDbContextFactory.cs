using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DymoEnergy.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class DymoEnergyDbContextFactory : IDesignTimeDbContextFactory<DymoEnergyDbContext>
{
    public DymoEnergyDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        DymoEnergyEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<DymoEnergyDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new DymoEnergyDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../DymoEnergy.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
