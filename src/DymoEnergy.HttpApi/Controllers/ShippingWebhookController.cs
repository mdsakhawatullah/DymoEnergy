using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DymoEnergy.Controllers;

/// <summary>
/// Endpoint couriers call with parcel status changes. It is anonymous by design; the request is
/// trusted only when it carries the courier's webhook secret (checked in the service).
/// </summary>
[ApiController]
[Route("api/shipping/webhook")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class ShippingWebhookController : DymoEnergyController
{
    private const int MaxBodyBytes = 64 * 1024;
    private readonly IShippingWebhookService _service;

    public ShippingWebhookController(IShippingWebhookService service)
    {
        _service = service;
    }

    [HttpPost("{courierAccountId:int}")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> Receive(int courierAccountId)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        var (status, replyHeaders) = await _service.HandleAsync(courierAccountId, headers, body);
        foreach (var (name, value) in replyHeaders) Response.Headers[name] = value;
        return StatusCode(status);
    }
}
