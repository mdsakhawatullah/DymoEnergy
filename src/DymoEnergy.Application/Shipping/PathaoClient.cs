using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Shipping;

/// <summary>Outcome of one HTTP call to Pathao, with what the call log needs.</summary>
public sealed class PathaoCall<T>
{
    public bool    Ok         { get; init; }
    public int?    StatusCode { get; init; }
    public int     DurationMs { get; init; }
    public string  Method     { get; init; } = "GET";
    public string  Endpoint   { get; init; } = string.Empty;
    public T?      Data       { get; init; }
    /// <summary>Human readable error from Pathao (or the network), never containing secrets.</summary>
    public string? Error      { get; init; }
}

public sealed record PathaoToken(string AccessToken, string? RefreshToken, int ExpiresInSeconds);

public sealed record PathaoOrderCreated(string ConsignmentId, string? MerchantOrderId, string Status, decimal DeliveryFee);

public sealed record PathaoOrderInfo(string ConsignmentId, string Status, string? StatusSlug, DateTime? UpdatedAt);

public sealed class PathaoOrderRequest
{
    public string  StoreId            { get; init; } = string.Empty;
    public string? MerchantOrderId    { get; init; }
    public string  RecipientName      { get; init; } = string.Empty;
    public string  RecipientPhone     { get; init; } = string.Empty;
    public string  RecipientAddress   { get; init; } = string.Empty;
    public int     DeliveryType       { get; init; } = 48;
    public int     ItemType           { get; init; } = 2;
    public string? SpecialInstruction { get; init; }
    public int     ItemQuantity       { get; init; } = 1;
    public decimal ItemWeightKg       { get; init; } = 0.5m;
    public int     AmountToCollect    { get; init; }
    public string? ItemDescription    { get; init; }
}

/// <summary>
/// Thin client for the Pathao Courier Merchant API (the "aladdin" endpoints).
/// It never logs or returns credentials; callers decide what to store.
/// </summary>
public class PathaoClient : ITransientDependency
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>One pooled client for the whole app; connections are recycled so DNS changes are picked up.</summary>
    private static readonly HttpClient Shared = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }) { Timeout = Timeout };

    /// <summary>Tests override this to talk to a fake server.</summary>
    protected virtual HttpClient Http => Shared;

    // ── Tokens ────────────────────────────────────────────────────────────

    public Task<PathaoCall<PathaoToken>> IssueTokenAsync(string baseUrl, string clientId, string clientSecret, string username, string password) =>
        SendAsync(baseUrl, HttpMethod.Post, "/aladdin/api/v1/issue-token", null, new Dictionary<string, object?>
        {
            ["client_id"] = clientId, ["client_secret"] = clientSecret, ["grant_type"] = "password",
            ["username"] = username, ["password"] = password,
        }, ReadToken);

    public Task<PathaoCall<PathaoToken>> RefreshTokenAsync(string baseUrl, string clientId, string clientSecret, string refreshToken) =>
        SendAsync(baseUrl, HttpMethod.Post, "/aladdin/api/v1/issue-token", null, new Dictionary<string, object?>
        {
            ["client_id"] = clientId, ["client_secret"] = clientSecret, ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, ReadToken);

    // ── Stores & locations ────────────────────────────────────────────────

    public Task<PathaoCall<List<PathaoStoreDto>>> GetStoresAsync(string baseUrl, string token) =>
        SendAsync(baseUrl, HttpMethod.Get, "/aladdin/api/v1/stores", token, null, root =>
            List(root).Select(s => new PathaoStoreDto
            {
                StoreId = Str(s, "store_id") ?? "",
                StoreName = Str(s, "store_name") ?? "",
                Address = Str(s, "store_address"),
                IsActive = Int(s, "is_active") == 1,
                IsDefault = Bool(s, "is_default_store"),
            }).ToList());

    public Task<PathaoCall<string>> CreateStoreAsync(string baseUrl, string token, CreatePathaoStoreDto input)
    {
        var body = new Dictionary<string, object?>
        {
            ["name"] = input.Name, ["contact_name"] = input.ContactName, ["contact_number"] = input.ContactNumber,
            ["address"] = input.Address, ["city_id"] = input.CityId, ["zone_id"] = input.ZoneId, ["area_id"] = input.AreaId,
        };
        if (!string.IsNullOrWhiteSpace(input.SecondaryContact)) body["secondary_contact"] = input.SecondaryContact;
        if (!string.IsNullOrWhiteSpace(input.OtpNumber)) body["otp_number"] = input.OtpNumber;
        return SendAsync(baseUrl, HttpMethod.Post, "/aladdin/api/v1/stores", token, body,
            root => Str(root, "message") ?? "Store created. Pathao approves new stores within about an hour.");
    }

    public Task<PathaoCall<List<PathaoLocationDto>>> GetCitiesAsync(string baseUrl, string token) =>
        SendAsync(baseUrl, HttpMethod.Get, "/aladdin/api/v1/city-list", token, null,
            root => List(root).Select(c => new PathaoLocationDto { Id = Int(c, "city_id") ?? 0, Name = (Str(c, "city_name") ?? "").Trim() }).ToList());

    public Task<PathaoCall<List<PathaoLocationDto>>> GetZonesAsync(string baseUrl, string token, int cityId) =>
        SendAsync(baseUrl, HttpMethod.Get, $"/aladdin/api/v1/cities/{cityId}/zone-list", token, null,
            root => List(root).Select(z => new PathaoLocationDto { Id = Int(z, "zone_id") ?? 0, Name = (Str(z, "zone_name") ?? "").Trim() }).ToList());

    public Task<PathaoCall<List<PathaoLocationDto>>> GetAreasAsync(string baseUrl, string token, int zoneId) =>
        SendAsync(baseUrl, HttpMethod.Get, $"/aladdin/api/v1/zones/{zoneId}/area-list", token, null,
            root => List(root).Select(a => new PathaoLocationDto
            {
                Id = Int(a, "area_id") ?? 0, Name = (Str(a, "area_name") ?? "").Trim(),
                HomeDeliveryAvailable = Bool(a, "home_delivery_available"), PickupAvailable = Bool(a, "pickup_available"),
            }).ToList());

    // ── Prices & orders ───────────────────────────────────────────────────

    public Task<PathaoCall<PathaoPriceDto>> GetPriceAsync(string baseUrl, string token, string storeId, PathaoPriceInputDto input) =>
        SendAsync(baseUrl, HttpMethod.Post, "/aladdin/api/v1/merchant/price-plan", token, new Dictionary<string, object?>
        {
            ["store_id"] = storeId, ["item_type"] = input.ItemType, ["delivery_type"] = input.DeliveryType,
            ["item_weight"] = input.WeightKg, ["recipient_city"] = input.CityId, ["recipient_zone"] = input.ZoneId,
        }, root =>
        {
            var d = Data(root);
            return new PathaoPriceDto
            {
                Price = Dec(d, "price") ?? 0, Discount = Dec(d, "discount") ?? 0, PromoDiscount = Dec(d, "promo_discount") ?? 0,
                AdditionalCharge = Dec(d, "additional_charge") ?? 0, CodPercentage = Dec(d, "cod_percentage") ?? 0,
                FinalPrice = Dec(d, "final_price") ?? 0,
            };
        });

    public Task<PathaoCall<PathaoOrderCreated>> CreateOrderAsync(string baseUrl, string token, PathaoOrderRequest o)
    {
        var body = new Dictionary<string, object?>
        {
            ["store_id"] = int.TryParse(o.StoreId, out var sid) ? sid : o.StoreId,
            ["recipient_name"] = o.RecipientName, ["recipient_phone"] = o.RecipientPhone, ["recipient_address"] = o.RecipientAddress,
            ["delivery_type"] = o.DeliveryType, ["item_type"] = o.ItemType, ["item_quantity"] = o.ItemQuantity,
            ["item_weight"] = o.ItemWeightKg.ToString("0.##", CultureInfo.InvariantCulture), ["amount_to_collect"] = o.AmountToCollect,
        };
        // Optional fields are left out entirely rather than sent as null (Pathao asks for this).
        if (!string.IsNullOrWhiteSpace(o.MerchantOrderId)) body["merchant_order_id"] = o.MerchantOrderId;
        if (!string.IsNullOrWhiteSpace(o.SpecialInstruction)) body["special_instruction"] = o.SpecialInstruction;
        if (!string.IsNullOrWhiteSpace(o.ItemDescription)) body["item_description"] = o.ItemDescription;

        return SendAsync(baseUrl, HttpMethod.Post, "/aladdin/api/v1/orders", token, body, root =>
        {
            var d = Data(root);
            return new PathaoOrderCreated(Str(d, "consignment_id") ?? "", Str(d, "merchant_order_id"),
                Str(d, "order_status") ?? "Pending", Dec(d, "delivery_fee") ?? 0);
        });
    }

    public Task<PathaoCall<PathaoOrderInfo>> GetOrderInfoAsync(string baseUrl, string token, string consignmentId) =>
        SendAsync(baseUrl, HttpMethod.Get, $"/aladdin/api/v1/orders/{Uri.EscapeDataString(consignmentId)}/info", token, null, root =>
        {
            var d = Data(root);
            DateTime? updated = DateTime.TryParse(Str(d, "updated_at"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var u) ? u : null;
            return new PathaoOrderInfo(Str(d, "consignment_id") ?? consignmentId, Str(d, "order_status") ?? "Unknown", Str(d, "order_status_slug"), updated);
        });

    // ── Plumbing ──────────────────────────────────────────────────────────

    private async Task<PathaoCall<T>> SendAsync<T>(string baseUrl, HttpMethod method, string path, string? token,
        Dictionary<string, object?>? body, Func<JsonElement, T> read)
    {
        var watch = Stopwatch.StartNew();
        var endpoint = path;
        try
        {
            if (!Uri.TryCreate(baseUrl?.TrimEnd('/') + path, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                return Fail<T>(method, endpoint, null, watch, "The base URL is missing or not a valid web address.");

            using var request = new HttpRequestMessage(method, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body != null) request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await Http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;

            JsonElement root = default;
            var parsed = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(text)) { root = JsonDocument.Parse(text).RootElement.Clone(); parsed = true; }
            }
            catch (JsonException) { /* not JSON — handled below */ }

            if (!response.IsSuccessStatusCode || (parsed && Str(root, "type") == "error"))
                return Fail<T>(method, endpoint, status, watch, parsed ? ErrorText(root, status) : $"Pathao answered {status} without a readable body.");
            if (!parsed)
                return Fail<T>(method, endpoint, status, watch, "Pathao answered without JSON.");

            return new PathaoCall<T> { Ok = true, StatusCode = status, DurationMs = (int)watch.ElapsedMilliseconds, Method = method.Method, Endpoint = endpoint, Data = read(root) };
        }
        catch (TaskCanceledException)
        {
            return Fail<T>(method, endpoint, null, watch, $"Pathao did not answer within {Timeout.TotalSeconds:0} seconds.");
        }
        catch (HttpRequestException ex)
        {
            return Fail<T>(method, endpoint, null, watch, "Could not reach Pathao: " + ex.Message);
        }
        catch (JsonException ex)
        {
            return Fail<T>(method, endpoint, null, watch, "Pathao's answer was not in the expected shape: " + ex.Message);
        }
    }

    private static PathaoCall<T> Fail<T>(HttpMethod method, string endpoint, int? status, Stopwatch watch, string error) =>
        new() { Ok = false, StatusCode = status, DurationMs = (int)watch.ElapsedMilliseconds, Method = method.Method, Endpoint = endpoint, Error = error };

    /// <summary>"Pathao's message — first field error", e.g. "Please fix the given errors — recipient_phone: must be 11 characters".</summary>
    private static string ErrorText(JsonElement root, int status)
    {
        var message = Str(root, "message") ?? $"Pathao answered {status}.";
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            foreach (var field in errors.EnumerateObject())
            {
                var first = field.Value.ValueKind == JsonValueKind.Array && field.Value.GetArrayLength() > 0
                    ? field.Value[0].ToString() : field.Value.ToString();
                return $"{message} — {field.Name}: {first}";
            }
        }
        return message;
    }

    private static PathaoToken ReadToken(JsonElement root) =>
        new(Str(root, "access_token") ?? throw new JsonException("No access_token in the answer."), Str(root, "refresh_token"), Int(root, "expires_in") ?? 3600);

    /// <summary>Pathao wraps payloads as { data: { data: [...] } } for lists and { data: {...} } for objects.</summary>
    private static JsonElement Data(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d) ? d : root;

    private static IEnumerable<JsonElement> List(JsonElement root)
    {
        var d = Data(root);
        if (d.ValueKind == JsonValueKind.Object && d.TryGetProperty("data", out var inner)) d = inner;
        return d.ValueKind == JsonValueKind.Array ? d.EnumerateArray().ToList() : Enumerable.Empty<JsonElement>();
    }

    private static string? Str(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    private static int? Int(JsonElement e, string name) =>
        int.TryParse(Str(e, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v
        : decimal.TryParse(Str(e, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? (int)d : null;

    private static decimal? Dec(JsonElement e, string name) =>
        decimal.TryParse(Str(e, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static bool Bool(JsonElement e, string name) =>
        Str(e, name) is "true" or "1";
}
