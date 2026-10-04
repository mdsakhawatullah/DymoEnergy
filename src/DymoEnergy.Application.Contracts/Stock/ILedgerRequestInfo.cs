using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Stock;

/// <summary>
/// The machine a ledger line is being written from. The web host fills this in; it stays empty for
/// background work and tests, where there is no request behind the change.
/// </summary>
public interface ILedgerRequestInfo
{
    string? IpAddress { get; }
    /// <summary>Browser and operating system, as the request reported them.</summary>
    string? Device { get; }
}

public class NullLedgerRequestInfo : ILedgerRequestInfo, ISingletonDependency
{
    public string? IpAddress => null;
    public string? Device => null;
}
