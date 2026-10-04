using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DymoEnergy.Shipping;

/// <summary>What each courier needs, plus small parsing helpers shared by the shipping code.</summary>
public static class CourierCatalog
{
    public record Field(string Key, string Label, string Help, bool IsSecret);

    // Keys the app writes itself; never shown as editable fields.
    public const string AccessToken    = "access_token";
    public const string RefreshToken   = "refresh_token";
    public const string TokenExpiresAt = "token_expires_at";
    public const string TokenIssuedAt  = "token_issued_at";
    public const string WebhookSecret  = "webhook_secret";
    public const string WebhookReply   = "webhook_reply_header";

    public static readonly IReadOnlyDictionary<CourierProvider, IReadOnlyList<Field>> Fields = new Dictionary<CourierProvider, IReadOnlyList<Field>>
    {
        [CourierProvider.Pathao] = new List<Field>
        {
            new("base_url", "Base URL (link)", "The address Pathao gave you. Sandbox and live are different addresses.", false),
            new("client_id", "Client ID", "From Pathao's merchant panel → Developer API.", true),
            new("client_secret", "Client secret", "Shown once when you create the key. Keep a copy somewhere safe.", true),
            new("username", "Merchant username", "The email you sign in to Pathao with.", false),
            new("password", "Password", "Pathao exchanges this for a token. Change it here whenever you change it there.", true),
            new(WebhookSecret, "Webhook secret", "The secret you set in Pathao's webhook settings. Status updates without it are refused.", true),
            new(WebhookReply, "Webhook reply header", "Optional, extra header to send back (Name: Value). Pathao's own webhook check is answered automatically.", false),
        },
        [CourierProvider.Steadfast] = new List<Field>
        {
            new("base_url", "Base URL (link)", "The API address from Steadfast.", false),
            new("api_key", "API key", "From the Steadfast merchant panel.", true),
            new("secret_key", "Secret key", "From the Steadfast merchant panel.", true),
        },
        [CourierProvider.RedX] = new List<Field>
        {
            new("base_url", "Base URL (link)", "The API address from RedX.", false),
            new("access_token", "Access token", "From RedX panel → Settings → API.", true),
        },
        [CourierProvider.ECourier] = new List<Field>
        {
            new("base_url", "Base URL (link)", "The API address from eCourier.", false),
            new("user_id", "User ID", "From the eCourier merchant panel.", false),
            new("api_key", "API key", "From the eCourier merchant panel.", true),
            new("api_secret", "API secret", "From the eCourier merchant panel.", true),
        },
        [CourierProvider.OwnDelivery] = new List<Field>(),
        [CourierProvider.Pickup] = new List<Field>(),
    };

    /// <summary>Keys that must be filled before the app can call the courier.</summary>
    public static readonly IReadOnlyDictionary<CourierProvider, string[]> Required = new Dictionary<CourierProvider, string[]>
    {
        [CourierProvider.Pathao] = new[] { "base_url", "client_id", "client_secret", "username", "password" },
        [CourierProvider.Steadfast] = new[] { "base_url", "api_key", "secret_key" },
        [CourierProvider.RedX] = new[] { "base_url", "access_token" },
        [CourierProvider.ECourier] = new[] { "base_url", "user_id", "api_key", "api_secret" },
        [CourierProvider.OwnDelivery] = Array.Empty<string>(),
        [CourierProvider.Pickup] = Array.Empty<string>(),
    };

    public static bool HasApi(CourierProvider p) => p == CourierProvider.Pathao;
    public static bool IsManual(CourierProvider p) => p is CourierProvider.OwnDelivery or CourierProvider.Pickup;

    public static Field? FindField(CourierProvider p, string key) =>
        Fields.TryGetValue(p, out var list) ? list.FirstOrDefault(f => f.Key == key) : null;

    /// <summary>Starter couriers created the first time the page opens.</summary>
    public static readonly (CourierProvider Provider, string Name, string Code, string Color)[] Seed =
    {
        (CourierProvider.Pathao, "Pathao Courier", "PTH", "#DB2777"),
        (CourierProvider.Steadfast, "Steadfast Courier", "STF", "#2563EB"),
        (CourierProvider.RedX, "RedX", "RDX", "#B42318"),
        (CourierProvider.ECourier, "eCourier", "ECR", "#1E40AF"),
        (CourierProvider.OwnDelivery, "Own delivery team", "OWN", "#0E6B3F"),
        (CourierProvider.Pickup, "Customer pickup", "PIK", "#5F6B63"),
    };

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>"+880 1712-345678" → "01712345678". Returns null when it cannot be an 11-digit Bangladeshi mobile.</summary>
    public static string? NormalizePhone(string? raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (digits.StartsWith("880") && digits.Length == 13) digits = digits[2..];
        else if (digits.StartsWith("88") && digits.Length == 13) digits = digits[2..];
        return digits.Length == 11 && digits.StartsWith("01") ? digits : null;
    }

    private static readonly Regex WeightPattern = new(@"(?<n>\d+(?:[.,]\d+)?)\s*(?<u>kg|kgs|kilo|kilogram|g|gm|gram|grams)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>"27 kg" → 27, "500 g" → 0.5, "1.2" → 1.2 (kg assumed). Null when there is no number.</summary>
    public static decimal? ParseWeightKg(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = WeightPattern.Match(text);
        if (!m.Success) return null;
        if (!decimal.TryParse(m.Groups["n"].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var n)) return null;
        var unit = m.Groups["u"].Value.ToLowerInvariant();
        return unit is "g" or "gm" or "gram" or "grams" ? n / 1000m : n;
    }

    /// <summary>Groups a courier's raw status ("Delivery_Failed", "In Transit"…) into the stages the page counts.</summary>
    public static string Stage(string? status)
    {
        var s = (status ?? "").ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        if (s.Contains("fail")) return "failed";
        if (s.Contains("return")) return "returned";
        if (s.Contains("cancel")) return "cancelled";
        if (s.Contains("deliver") && !s.Contains("assign") && !s.Contains("out for")) return "delivered";
        if (s.Contains("transit") || s.Contains("hub") || s.Contains("sort") || s.Contains("out for") || s.Contains("assign")) return "transit";
        if (s.Contains("picked")) return "picked";
        return "ready";
    }

    /// <summary>"•••• wQ4a" — enough to recognise a key without exposing it.</summary>
    public static string Mask(string value) =>
        value.Length <= 4 ? "••••" : "•••••••••• " + value[^4..];
}
