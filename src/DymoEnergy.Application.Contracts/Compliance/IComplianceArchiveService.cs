using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Compliance;

/// <summary>Builds the "Download all (ZIP)" archive of every attached compliance file.</summary>
public interface IComplianceArchiveService : ITransientDependency
{
    /// <summary>Returns a seekable stream holding the zip, or null when there is nothing to download.</summary>
    Task<Stream?> BuildArchiveAsync();
}
