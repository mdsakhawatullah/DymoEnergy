using DymoEnergy.Stock;
using Volo.Abp.AspNetCore.WebClientInfo;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Controllers;

/// <summary>Takes the caller's address and device from the live request, for the stock ledger.</summary>
[Dependency(ReplaceServices = true)]
public class HttpLedgerRequestInfo : ILedgerRequestInfo, ITransientDependency
{
    private readonly IWebClientInfoProvider _client;

    public HttpLedgerRequestInfo(IWebClientInfoProvider client)
    {
        _client = client;
    }

    public string? IpAddress => _client.ClientIpAddress;

    public string? Device => Join(_client.BrowserInfo, _client.DeviceInfo);

    private static string? Join(string? browser, string? device) =>
        string.IsNullOrWhiteSpace(device) ? browser
        : string.IsNullOrWhiteSpace(browser) ? device
        : $"{browser} · {device}";
}
