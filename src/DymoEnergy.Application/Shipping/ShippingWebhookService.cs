using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace DymoEnergy.Shipping;

/// <summary>
/// Status updates pushed by Pathao. A request is only accepted when it carries the webhook secret
/// saved on the courier; nothing is changed otherwise.
/// </summary>
public class ShippingWebhookService : IShippingWebhookService
{
    public const string SignatureHeader = "X-PATHAO-Signature";

    private readonly IRepository<CourierAccount, int> _accounts;
    private readonly IRepository<Shipment, int> _shipments;
    private readonly CourierVault _vault;
    private readonly CourierLogWriter _log;
    private readonly IClock _clock;

    public ShippingWebhookService(IRepository<CourierAccount, int> accounts, IRepository<Shipment, int> shipments,
        CourierVault vault, CourierLogWriter log, IClock clock)
    {
        _accounts = accounts; _shipments = shipments; _vault = vault; _log = log; _clock = clock;
    }

    [UnitOfWork]
    public virtual async Task<(int StatusCode, Dictionary<string, string> Headers)> HandleAsync(int courierAccountId, IDictionary<string, string> headers, string body)
    {
        var reply = new Dictionary<string, string>();
        var account = await _accounts.FirstOrDefaultAsync(a => a.Id == courierAccountId);
        if (account == null || account.Provider != CourierProvider.Pathao) return (404, reply);
        var env = account.ActiveEnvironment;

        var secret = await _vault.GetAsync(account.Id, env, CourierCatalog.WebhookSecret);
        var sent = headers.FirstOrDefault(h => string.Equals(h.Key, SignatureHeader, StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(sent) || !FixedTimeEquals(secret, sent))
        {
            await _log.WriteAsync(account.Id, env, "Status update refused", "POST", $"/api/shipping/webhook/{account.Id}", 401, null,
                string.IsNullOrEmpty(secret) ? "No webhook secret saved for this courier" : "Wrong or missing signature", true);
            return (401, reply);
        }

        // Pathao may expect a header back (for example when it verifies the webhook URL); it is configurable.
        var replyHeader = await _vault.GetAsync(account.Id, env, CourierCatalog.WebhookReply);
        if (!string.IsNullOrWhiteSpace(replyHeader) && replyHeader.IndexOf(':') is var i and > 0)
            reply[replyHeader[..i].Trim()] = replyHeader[(i + 1)..].Trim();

        string? consignment = null, status = null, eventName = null;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;
            consignment = Read(root, "consignment_id");
            status = Read(root, "order_status") ?? Read(root, "status");
            eventName = Read(root, "event");
        }
        catch (JsonException)
        {
            await _log.WriteAsync(account.Id, env, "Status update", "POST", $"/api/shipping/webhook/{account.Id}", 400, null, "Body was not JSON", true);
            return (400, reply);
        }

        // An event like "order.delivered" carries the status in its name.
        status ??= eventName?.Split('.').LastOrDefault()?.Replace('-', '_');
        var note = "No parcel in the message";
        if (!string.IsNullOrEmpty(consignment))
        {
            var shipment = await _shipments.FirstOrDefaultAsync(s => s.ConsignmentId == consignment && s.CourierAccountId == account.Id);
            if (shipment == null) note = $"Unknown consignment {consignment}";
            else if (!string.IsNullOrEmpty(status))
            {
                shipment.Status = status.Length > 120 ? status[..120] : status;
                shipment.StatusAt = _clock.Now;
                await _shipments.UpdateAsync(shipment, autoSave: true);
                note = $"{shipment.Status}, consignment {consignment}";
            }
            else note = $"No status for {consignment}";
        }
        else if (!string.IsNullOrEmpty(eventName)) note = $"Event {eventName}";

        account.LastWebhookAt = _clock.Now;
        account.LastWebhookNote = note;
        await _accounts.UpdateAsync(account, autoSave: true);
        await _log.WriteAsync(account.Id, env, "Status update", "POST", $"/api/shipping/webhook/{account.Id}", 202, null, note);
        return (202, reply);
    }

    private static string? Read(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
            ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind == JsonValueKind.Number ? v.GetRawText() : null
            : null;

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
