using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Orders;
using DymoEnergy.Products;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Shipping;

/// <summary>What the shipping code needs to know about one order.</summary>
public sealed class OrderShippingFacts
{
    public int     OrderId      { get; init; }
    public string  OrderNumber  { get; init; } = string.Empty;
    public DateTime OrderDate   { get; init; }
    public string  CustomerName { get; init; } = string.Empty;
    public string? Phone        { get; init; }
    public string  Address      { get; init; } = string.Empty;
    public string  ItemsText    { get; init; } = string.Empty;
    public List<string> ProductNames { get; init; } = new();
    public decimal TotalWeightKg { get; init; }
    /// <summary>Heaviest single unit in the order.</summary>
    public decimal MaxItemKg    { get; init; }
    public bool    WeightGuessed { get; init; }
    public decimal Cod          { get; init; }
    public bool    NeedsInstallation { get; init; }
}

/// <summary>Builds <see cref="OrderShippingFacts"/> from orders, their lines and product weights.</summary>
public class OrderFactsBuilder : ITransientDependency
{
    private readonly IRepository<OrderItem, int> _items;
    private readonly IRepository<Product, int> _products;

    public OrderFactsBuilder(IRepository<OrderItem, int> items, IRepository<Product, int> products)
    {
        _items = items; _products = products;
    }

    public async Task<List<OrderShippingFacts>> BuildAsync(List<Order> orders, decimal defaultWeightKg)
    {
        if (orders.Count == 0) return new List<OrderShippingFacts>();
        var ids = orders.Select(o => o.Id).ToList();
        var items = await _items.GetListAsync(i => ids.Contains(i.OrderId));
        var productIds = items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
        var weights = (await _products.GetListAsync(p => productIds.Contains(p.Id))).ToDictionary(p => p.Id, p => CourierCatalog.ParseWeightKg(p.Weight));

        return orders.Select(o =>
        {
            var lines = items.Where(i => i.OrderId == o.Id).OrderBy(i => i.DisplayOrder).ToList();
            decimal known = 0, max = 0; var guessed = false;
            foreach (var l in lines)
            {
                var w = l.ProductId.HasValue && weights.TryGetValue(l.ProductId.Value, out var kg) ? kg : null;
                if (w.HasValue) { known += w.Value * (decimal)l.Quantity; max = Math.Max(max, w.Value); }
                else guessed = true;
            }
            var total = known > 0 ? known + (guessed ? defaultWeightKg : 0) : defaultWeightKg;

            return new OrderShippingFacts
            {
                OrderId = o.Id, OrderNumber = o.OrderNumber ?? $"#{o.Id}", OrderDate = o.OrderDate,
                CustomerName = (o.DeliveryContact ?? o.CustomerName ?? "").Trim(), Phone = o.DeliveryPhone ?? o.CustomerPhone,
                Address = (o.DeliveryAddress ?? o.BillingAddress ?? "").Trim(),
                ItemsText = string.Join(", ", lines.Select(l => $"{l.Quantity:0.##} × {l.ProductName}")),
                ProductNames = lines.Select(l => l.ProductName ?? "").ToList(),
                TotalWeightKg = Math.Round(total, 2), MaxItemKg = Math.Round(max, 2), WeightGuessed = guessed || known == 0,
                Cod = Math.Max(0, Math.Round((decimal)o.BalanceDue, 2)),
                NeedsInstallation = o.ShipmentType == OrderShipmentType.DeliveryAndInstall,
            };
        }).ToList();
    }
}

/// <summary>Evaluates courier rules top to bottom; first enabled match wins.</summary>
public static class CourierRuleEngine
{
    public static CourierRule? FirstMatch(IEnumerable<CourierRule> rules, OrderShippingFacts f) =>
        rules.Where(r => r.IsEnabled).OrderBy(r => r.Order).ThenBy(r => r.Id).FirstOrDefault(r => Matches(r, f));

    public static bool Matches(CourierRule r, OrderShippingFacts f)
    {
        var checks = new List<bool>();
        if (!string.IsNullOrWhiteSpace(r.ProductKeyword))
            checks.Add(Words(r.ProductKeyword).Any(w => f.ProductNames.Any(n => n.Contains(w, StringComparison.OrdinalIgnoreCase))));
        if (r.AnyItemOverKg.HasValue) checks.Add(f.MaxItemKg > r.AnyItemOverKg.Value);
        if (r.TotalWeightUnderKg.HasValue) checks.Add(f.TotalWeightKg < r.TotalWeightUnderKg.Value);
        if (!string.IsNullOrWhiteSpace(r.AddressContains))
            checks.Add(Words(r.AddressContains).Any(w => f.Address.Contains(w, StringComparison.OrdinalIgnoreCase)));
        if (r.CodOver.HasValue) checks.Add(f.Cod > r.CodOver.Value);
        if (r.NeedsInstallation) checks.Add(f.NeedsInstallation);

        if (checks.Count == 0) return true;            // a rule with no conditions catches everything
        return r.MatchAny ? checks.Any(c => c) : checks.All(c => c);
    }

    /// <summary>"If the order has a product named “panel”, or any item over 20 kg" — the left half of the sentence.</summary>
    public static string DescribeIf(CourierRule r)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(r.ProductKeyword))
            parts.Add("a product named " + string.Join(" or ", Words(r.ProductKeyword).Select(w => $"“{w}”")));
        if (r.AnyItemOverKg.HasValue) parts.Add($"any item over {Num(r.AnyItemOverKg.Value)} kg");
        if (r.TotalWeightUnderKg.HasValue) parts.Add($"a total weight under {Num(r.TotalWeightUnderKg.Value)} kg");
        if (!string.IsNullOrWhiteSpace(r.AddressContains))
            parts.Add("an address in " + string.Join(" or ", Words(r.AddressContains)));
        if (r.CodOver.HasValue) parts.Add($"cash on delivery over ৳{Num(r.CodOver.Value)}");
        if (r.NeedsInstallation) parts.Add("installation");

        if (parts.Count == 0) return "anything else";
        var joiner = r.MatchAny ? ", or " : ", and ";
        return "the order has " + string.Join(joiner, parts);
    }

    public static string DescribeThen(CourierRule r, string? courierName)
    {
        var head = r.NoParcel ? "do not create a courier parcel" : $"send it with {courierName ?? "the chosen courier"}";
        return string.IsNullOrWhiteSpace(r.ThenNote) ? head : $"{head} — {r.ThenNote}";
    }

    private static IEnumerable<string> Words(string s) =>
        s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Num(decimal v) => v.ToString("#,##0.##", CultureInfo.InvariantCulture);
}
