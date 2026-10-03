using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Finance;

/// <summary>Builds the "Export for accountant" ZIP of CSV files for a period.</summary>
public interface IFinanceExportService : ITransientDependency
{
    Task<(Stream Stream, string FileName)> BuildAsync(string? period);
}
