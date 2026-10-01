using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DymoEnergy.Analytics;

/// <summary>
/// One sale, regardless of where it was recorded. Sales invoices (showroom / POS / field)
/// and web orders are normalised into this shape so every tab can treat them alike.
/// </summary>
internal class SaleRecord
{
    public const string OnlineChannel = "Online shop";

    public string    Source   { get; init; } = "invoice";   // invoice | order
    public int       Id       { get; init; }
    public string?   Number   { get; init; }
    /// <summary>Stored sale date — compared with the (UTC) filter range.</summary>
    public DateTime  Date     { get; init; }
    /// <summary>Bangladesh-local sale date for day grouping.</summary>
    public DateTime  LocalDate    { get; init; }
    /// <summary>Bangladesh-local creation time for the hour heatmap.</summary>
    public DateTime  LocalCreated { get; init; }
    public string    Channel  { get; init; } = "Showroom";

    public string?   CustomerName { get; init; }
    public string?   Phone        { get; init; }
    public string?   CustomerKey  { get; init; }
    public string?   Location     { get; init; }

    public double    Total        { get; init; }
    public double    Paid         { get; init; }
    public double    Balance      { get; init; }
    public double    Tax          { get; init; }
    public double    LineDiscount { get; init; }
    public double    AdditionalDiscount { get; init; }
    public string?   DiscountNote { get; init; }
    public string?   Voucher      { get; init; }
    public double    VoucherAmount { get; init; }

    public DateTime? DueDate      { get; init; }
    public bool      IsCod        { get; init; }
    public Guid?     CreatorId    { get; init; }
    /// <summary>Last payment method label ("bKash", "Cash"…), if any.</summary>
    public string?   PaymentMethod { get; init; }

    /// <summary>Counts towards sales (not draft / cancelled / refunded).</summary>
    public bool      Counted      { get; init; }
    /// <summary>Money still to collect on a live sale.</summary>
    public bool      IsOpen       { get; init; }
    public bool      Refunded     { get; init; }
    public DateTime? RefundDate   { get; init; }

    public List<SaleItem> Items { get; init; } = new();

    public string Summary =>
        Items.Count == 0 ? (Number ?? "Sale")
        : Items.Count == 1 ? Items[0].Name
        : $"{Items[0].Name} + {Items.Count - 1} more";

    public string DisplayName => string.IsNullOrWhiteSpace(CustomerName) ? "Walk-in customer" : CustomerName!;
}

internal class SaleItem
{
    public int?    ProductId { get; init; }
    public string  Name      { get; init; } = "Item";
    public double  Quantity  { get; init; }
    public double  Total     { get; init; }
    public string? Warranty  { get; init; }
    public string? Serials   { get; init; }
    public string? Sku       { get; init; }
    /// <summary>VAT inside this line (VAT-inclusive or not).</summary>
    public double  Tax       { get; init; }
}

internal class PaymentRecord
{
    public DateTime Date    { get; init; }
    public double   Amount  { get; init; }
    public string   Method  { get; init; } = "Other";
    public string   Channel { get; init; } = "Showroom";
    public string?  Invoice { get; init; }
    public string?  Customer { get; init; }
    public string?  Reference { get; init; }
}

internal class ProductInfo
{
    public int     Id       { get; init; }
    public string  Name     { get; init; } = string.Empty;
    public string  Category { get; init; } = "Other";
    public int     Stock    { get; init; }
    public double  Price    { get; init; }
    public bool    IsActive { get; init; }
    public string? Sku      { get; init; }
    /// <summary>Supplier / vendor name from the product record.</summary>
    public string? Vendor   { get; init; }
}

internal class QuoteInfo
{
    public DateTime Date     { get; init; }
    public int      Status   { get; init; }   // QuoteRequestStatus
    public string?  PhoneKey { get; init; }
    public string   Name     { get; init; } = string.Empty;
    public string?  Interest { get; init; }
    public int      Id       { get; init; }
    public string?  Size     { get; init; }
    public string?  Note     { get; init; }
}

/// <summary>Everything a single analytics request needs, loaded once and filtered in memory.</summary>
internal class AnalyticsSnapshot
{
    public DateTime Now       { get; init; }
    public DateTime Today     { get; init; }   // Bangladesh-local date
    public DateTime From      { get; init; }
    public DateTime To        { get; init; }
    public DateTime PrevFrom  { get; init; }
    public DateTime PrevTo    { get; init; }
    public string?  Channel   { get; init; }

    /// <summary>All loaded sales (history window + open balances), channel-filtered.</summary>
    public List<SaleRecord>    Sales     { get; init; } = new();
    public List<PaymentRecord> Payments  { get; init; } = new();
    public List<ProductInfo>   Products  { get; init; } = new();
    public List<QuoteInfo>     Quotes    { get; init; } = new();
    /// <summary>Customer keys that bought before the loaded history window.</summary>
    public HashSet<string>     OlderCustomerKeys { get; init; } = new();
    public Dictionary<Guid, string> UserNames { get; init; } = new();

    public IEnumerable<SaleRecord> Counted => Sales.Where(s => s.Counted);
    public IEnumerable<SaleRecord> InPeriod => Counted.Where(s => s.Date >= From && s.Date <= To);
    public IEnumerable<SaleRecord> InPrevious => Counted.Where(s => s.Date >= PrevFrom && s.Date < PrevTo);
    public IEnumerable<SaleRecord> Open => Sales.Where(s => s.IsOpen && s.Balance > 0.005);

    public int DaysInPeriod => Math.Max(1, (int)Math.Ceiling((Min(To, Now) - From).TotalDays));

    private Dictionary<int, ProductInfo>? _byId;
    private Dictionary<string, ProductInfo>? _byName;

    /// <summary>Resolve an item to a catalogue product — by id, else by exact name.</summary>
    public ProductInfo? FindProduct(SaleItem item)
    {
        _byId   ??= Products.ToDictionary(p => p.Id);
        _byName ??= Products.GroupBy(p => p.Name.Trim().ToLowerInvariant())
                            .ToDictionary(g => g.Key, g => g.First());
        if (item.ProductId.HasValue && _byId.TryGetValue(item.ProductId.Value, out var p)) return p;
        return _byName.TryGetValue(item.Name.Trim().ToLowerInvariant(), out var n) ? n : null;
    }

    public string CategoryOf(SaleItem item)
    {
        var product = FindProduct(item);
        if (product != null) return product.Category;
        return AnalyticsText.IsService(item.Name) ? "Services" : "Other";
    }

    public static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}

/// <summary>Small parsing helpers shared by the tabs and reports.</summary>
internal static class AnalyticsText
{
    private static readonly Regex PanelWatts  = new(@"(\d{2,4})\s*W(p)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PanelWord   = new(@"panel|module|perc|mono|poly", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ServiceWord = new(@"install|service|labou?r|commission|survey|transport|delivery", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Years  = new(@"(\d+(?:\.\d+)?)\s*-?\s*(yr|year)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Months = new(@"(\d+)\s*-?\s*(mo|month)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsService(string name) => ServiceWord.IsMatch(name);

    /// <summary>Watts of a solar panel line ("Mono PERC Panel 550W" → 550), or null if not a panel.</summary>
    public static int? PanelWattage(string name)
    {
        if (!PanelWord.IsMatch(name)) return null;
        var m = PanelWatts.Match(name);
        return m.Success && int.TryParse(m.Groups[1].Value, out var w) && w >= 50 ? w : null;
    }

    /// <summary>"25-yr output warranty" → sale date + 25 years; null when unparseable.</summary>
    public static DateTime? WarrantyEnd(string? warranty, DateTime saleDate)
    {
        if (string.IsNullOrWhiteSpace(warranty)) return null;
        var y = Years.Match(warranty);
        if (y.Success && double.TryParse(y.Groups[1].Value, out var years))
            return saleDate.AddMonths((int)Math.Round(years * 12));
        var m = Months.Match(warranty);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var months))
            return saleDate.AddMonths(months);
        return null;
    }

    /// <summary>Stable identity for a customer: last 10 phone digits, else the name.</summary>
    public static string? CustomerKey(string? phone, string? name)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length >= 10) return "p:" + digits[^10..];
        var n = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (n.Length == 0 || n.StartsWith("walk-in") || n.StartsWith("walk in")) return null;
        return "n:" + n;
    }

    /// <summary>City from a free-text address — the last meaningful comma segment.</summary>
    public static string? Location(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => p.Length > 0 && !p.Equals("Bangladesh", StringComparison.OrdinalIgnoreCase))
                           .Where(p => !p.All(c => char.IsDigit(c) || c == '-' || c == ' '))   // drop post codes
                           .ToList();
        if (parts.Count == 0) return null;
        var city = parts[^1];
        return char.ToUpperInvariant(city[0]) + city[1..];
    }

    public static int CountSerials(string? serials) =>
        (serials ?? string.Empty).Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Count(s => s.Trim().Length > 0);

    /// <summary>% change, or null when there is no baseline.</summary>
    public static double? Change(double current, double previous) =>
        previous > 0.005 ? Math.Round((current - previous) / previous * 100, 1) : null;

    public static List<NamedValueDto> WithShares(IEnumerable<NamedValueDto> items)
    {
        var list  = items.ToList();
        var total = list.Sum(i => i.Value);
        foreach (var i in list) i.Share = total > 0 ? Math.Round(i.Value / total, 4) : 0;
        return list;
    }
}
