using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Status updates pushed by Pathao. Parcel updates are only accepted when they carry the webhook secret
/// saved on the courier. Pathao's one-off "webhook_integration" check is answered with the header Pathao
/// expects, so the URL can be saved in the merchant panel.
/// </summary>
public class ShippingWebhookService : IShippingWebhookService
{
    public const string SignatureHeader = "X-PATHAO-Signature";
    public const string IntegrationEvent = "webhook_integration";
    /// <summary>Fixed values from Pathao's webhook documentation (the same for every merchant).</summary>
    public const string IntegrationHeader = "X-Pathao-Merchant-Webhook-Integration-Secret";
    public const string IntegrationValue = "f3992ecc-59da-4cbe-a049-a13da2018d51";

    private readonly IRepository<CourierAccount, int> _accounts;
    private readonly IRepository<Shipment, int> _shipments;
    private readonly IRepository<ShipmentEvent, int> _events;
    private readonly CourierVault _vault;
    private readonly CourierLogWriter _log;
    private readonly IClock _clock;

    public ShippingWebhookService(IRepository<CourierAccount, int> accounts, IRepository<Shipment, int> shipments,
        IRepository<ShipmentEvent, int> events, CourierVault vault, CourierLogWriter log, IClock clock)
    {
        _accounts = accounts; _shipments = shipments; _events = events; _vault = vault; _log = log; _clock = clock;
    }

    [UnitOfWork]
    public virtual async Task<(int StatusCode, Dictionary<string, string> Headers)> HandleAsync(int courierAccountId, IDictionary<string, string> headers, string body)
    {
        var reply = new Dictionary<string, string>();
        var account = await _accounts.FirstOrDefaultAsync(a => a.Id == courierAccountId);
        if (account == null || account.Provider != CourierProvider.Pathao) return (404, reply);
        var env = account.ActiveEnvironment;
        var path = $"/api/shipping/webhook/{account.Id}";

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            await _log.WriteAsync(account.Id, env, "Status update", "POST", path, 400, null, "Body was not JSON", true);
            return (400, reply);
        }

        var eventName = Read(root, "event");
        var secret = await _vault.GetAsync(account.Id, env, CourierCatalog.WebhookSecret);
        var sent = headers.FirstOrDefault(h => string.Equals(h.Key, SignatureHeader, StringComparison.OrdinalIgnoreCase)).Value;
        var signed = !string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(sent) && FixedTimeEquals(secret, sent);

        // Pathao checks the URL once when you save it: reply 202 with its fixed header. Nothing is changed,
        // so this is answered even before the secret is saved here (the header value is public).
        if (string.Equals(eventName, IntegrationEvent, StringComparison.OrdinalIgnoreCase))
        {
            reply[IntegrationHeader] = IntegrationValue;
            AddCustomReply(reply, await _vault.GetAsync(account.Id, env, CourierCatalog.WebhookReply));
            var note = signed ? "Webhook connected" : string.IsNullOrEmpty(secret)
                ? "Webhook connected — now save the same webhook secret here, or parcel updates will be refused"
                : "Webhook connected — but the secret Pathao sent does not match the one saved here";
            account.LastWebhookAt = _clock.Now;
            account.LastWebhookNote = note;
            await _accounts.UpdateAsync(account, autoSave: true);
            await _log.WriteAsync(account.Id, env, "Webhook check", "POST", path, 202, null, note, !signed);
            return (202, reply);
        }

        if (!signed)
        {
            await _log.WriteAsync(account.Id, env, "Status update refused", "POST", path, 401, null,
                string.IsNullOrEmpty(secret) ? "No webhook secret saved for this courier" : "Wrong or missing signature", true);
            return (401, reply);
        }
        AddCustomReply(reply, await _vault.GetAsync(account.Id, env, CourierCatalog.WebhookReply));

        var consignment = Read(root, "consignment_id");
        var status = Read(root, "order_status") ?? Read(root, "status") ?? StatusFromEvent(eventName);
        var result = "No parcel in the message";
        if (!string.IsNullOrEmpty(consignment))
        {
            var shipment = await _shipments.FirstOrDefaultAsync(s => s.ConsignmentId == consignment && s.CourierAccountId == account.Id);
            if (shipment == null) result = $"Unknown consignment {consignment}";
            else if (string.IsNullOrEmpty(status)) result = $"No status for {consignment}";
            else
            {
                var at = ReadTime(root, "updated_at") ?? ReadTime(root, "timestamp") ?? _clock.Now;
                // Webhooks can arrive out of order; an older event goes into the history but does not overwrite the current status.
                if (shipment.StatusAt == null || at >= shipment.StatusAt.Value)
                {
                    shipment.Status = Cut(status, 120)!;
                    shipment.StatusSlug = Cut(eventName, 120);
                    shipment.StatusAt = at;
                }
                if (ReadDecimal(root, "delivery_fee") is { } fee && fee > 0) shipment.DeliveryFee = fee;
                await _shipments.UpdateAsync(shipment, autoSave: true);

                await _events.InsertAsync(new ShipmentEvent
                {
                    ShipmentId = shipment.Id, Time = at, Status = Cut(status, 128)!, Source = "webhook", Event = Cut(eventName, 64),
                    Note = Cut(Read(root, "reason") ?? Read(root, "failed_reason") ?? Read(root, "note"), 512),
                    CollectedAmount = ReadDecimal(root, "collected_amount"),
                }, autoSave: true);
                result = $"{status}, consignment {consignment}";
            }
        }
        else if (!string.IsNullOrEmpty(eventName)) result = $"Event {eventName}";

        account.LastWebhookAt = _clock.Now;
        account.LastWebhookNote = result;
        await _accounts.UpdateAsync(account, autoSave: true);
        await _log.WriteAsync(account.Id, env, "Status update", "POST", path, 202, null, result);
        return (202, reply);
    }

    /// <summary>"order.pickup-requested" → "Pickup Requested".</summary>
    public static string? StatusFromEvent(string? eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return null;
        var last = eventName.Split('.').Last().Replace('-', ' ').Replace('_', ' ').Trim();
        return last.Length == 0 ? null : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(last.ToLowerInvariant());
    }

    private static void AddCustomReply(Dictionary<string, string> reply, string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && configured.IndexOf(':') is var i and > 0)
            reply[configured[..i].Trim()] = configured[(i + 1)..].Trim();
    }

    private static string? Cut(string? s, int max) => s == null ? null : s.Length > max ? s[..max] : s;

    private static string? Read(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
            ? v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind == JsonValueKind.Number ? v.GetRawText() : null
            : null;

    private static decimal? ReadDecimal(JsonElement e, string name) =>
        decimal.TryParse(Read(e, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static DateTime? ReadTime(JsonElement e, string name) =>
        DateTime.TryParse(Read(e, name), CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
