using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Finance;
using DymoEnergy.Orders;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Shipping;

[Authorize(DymoEnergyPermissions.Shipping.Default)]
public class ShippingConfigAppService : ApplicationService, IShippingConfigAppService
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private const decimal Eps = 0.5m;   // payouts within half a taka count as matched

    private readonly IRepository<ShippingSetting, int>  _settings;
    private readonly IRepository<ShippingZone, int>     _zones;
    private readonly IRepository<ShippingListItem, int> _items;
    private readonly IRepository<CourierRule, int>      _rules;
    private readonly IRepository<CourierPayout, int>    _payouts;
    private readonly IRepository<CourierAccount, int>   _accounts;
    private readonly IRepository<Shipment, int>         _shipments;
    private readonly IRepository<Order, int>            _orders;
    private readonly IRepository<FinanceAccount, int>   _financeAccounts;
    private readonly IRepository<FinanceTransaction, int> _financeTxs;
    private readonly OrderFactsBuilder _facts;
    private readonly IShippingAppService _shipping;

    public ShippingConfigAppService(
        IRepository<ShippingSetting, int> settings, IRepository<ShippingZone, int> zones, IRepository<ShippingListItem, int> items,
        IRepository<CourierRule, int> rules, IRepository<CourierPayout, int> payouts, IRepository<CourierAccount, int> accounts,
        IRepository<Shipment, int> shipments, IRepository<Order, int> orders,
        IRepository<FinanceAccount, int> financeAccounts, IRepository<FinanceTransaction, int> financeTxs, OrderFactsBuilder facts,
        IShippingAppService shipping)
    {
        _settings = settings; _zones = zones; _items = items; _rules = rules; _payouts = payouts; _accounts = accounts;
        _shipments = shipments; _orders = orders; _financeAccounts = financeAccounts; _financeTxs = financeTxs; _facts = facts; _shipping = shipping;
    }

    // ══ CHARGES & ZONES ══════════════════════════════════════════════════════

    public async Task<ChargesPageDto> GetChargesAsync()
    {
        await EnsureSeedAsync();
        var items = await _items.GetListAsync();
        var pathao = await PathaoAccountAsync();
        return new ChargesPageDto
        {
            PathaoAccountId = pathao?.Id, PathaoAccountName = pathao?.DisplayName,
            Setting = MapSetting(await LoadSettingAsync()),
            Zones = (await _zones.GetListAsync()).OrderBy(z => z.Order).ThenBy(z => z.Id).Select(MapZone).ToList(),
            BigItems = ItemsOf(items, ShippingItemKind.BigItem),
            ReturnPolicies = ItemsOf(items, ShippingItemKind.ReturnPolicy),
        };
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<ShippingSettingDto> UpdateSettingAsync(ShippingSettingDto input)
    {
        await EnsureSeedAsync();
        var s = await LoadSettingAsync();
        s.FreeDeliveryEnabled = input.FreeDeliveryEnabled; s.FreeDeliveryOver = Math.Round(input.FreeDeliveryOver, 2);
        s.WeightChargeEnabled = input.WeightChargeEnabled; s.WeightIncludedKg = input.WeightIncludedKg;
        s.CodFeePassed = input.CodFeePassed; s.CodFeePercent = input.CodFeePercent; s.CustomerChoosesCourier = input.CustomerChoosesCourier;
        s.OwnTruckPerKm = Math.Round(input.OwnTruckPerKm, 2); s.MinTripCharge = Math.Round(input.MinTripCharge, 2); s.NoCashAlertDays = input.NoCashAlertDays;
        await _settings.UpdateAsync(s, autoSave: true);
        return MapSetting(s);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<ShippingZoneDto> CreateZoneAsync(CreateUpdateShippingZoneDto input)
    {
        var z = new ShippingZone();
        ApplyZone(z, input);
        if (input.Order <= 0) z.Order = (await _zones.GetListAsync()).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
        await _zones.InsertAsync(z, autoSave: true);
        return MapZone(z);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<ShippingZoneDto> UpdateZoneAsync(int id, CreateUpdateShippingZoneDto input)
    {
        var z = await _zones.GetAsync(id);
        ApplyZone(z, input);
        await _zones.UpdateAsync(z, autoSave: true);
        return MapZone(z);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task DeleteZoneAsync(int id) => await _zones.DeleteAsync(id, autoSave: true);

    /// <summary>
    /// Courier cost per zone straight from Pathao's price plan: the price for the included weight at the
    /// zone's sample Pathao location, and the extra for one more kg. A zone the customer price was never
    /// set for (0) starts at Pathao's price, so nothing is charged below cost by accident.
    /// </summary>
    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<RefreshZonePricesResultDto> RefreshZonePricesAsync(RefreshZonePricesInput request)
    {
        var zoneId = request.ZoneId;
        var pathao = await PathaoAccountAsync() ?? throw new UserFriendlyException("Switch on a Pathao courier and save its keys first.");
        var setting = await LoadSettingAsync();
        var zones = (await _zones.GetListAsync(z => zoneId == null || z.Id == zoneId)).OrderBy(z => z.Order).ToList();
        if (zoneId != null && zones.Count == 0) throw new UserFriendlyException("That zone no longer exists.");

        // Pathao prices from 0.5 to 10 kg.
        var baseKg = Math.Clamp(setting.WeightIncludedKg, 0.5m, 9m);
        var result = new RefreshZonePricesResultDto
        {
            Source = $"{pathao.DisplayName} · {(pathao.ActiveEnvironment == CourierEnvironment.Live ? "live" : "sandbox")} · "
                   + $"{(pathao.DefaultDeliveryType == 12 ? "on demand" : "normal delivery")} · {(pathao.DefaultItemType == 1 ? "document" : "parcel")} · {baseKg:0.##} kg",
        };

        foreach (var z in zones)
        {
            if (z.PathaoCityId is not { } city || z.PathaoZoneId is not { } area)
            {
                result.Zones.Add(new ZonePriceResultDto { ZoneId = z.Id, Name = z.Name, Message = "No Pathao location chosen." });
                continue;
            }
            try
            {
                var input = new PathaoPriceInputDto { ItemType = pathao.DefaultItemType, DeliveryType = pathao.DefaultDeliveryType, CityId = city, ZoneId = area, WeightKg = baseKg };
                var first = await _shipping.GetPathaoPriceAsync(pathao.Id, input);
                input.WeightKg = baseKg + 1;
                var next = await _shipping.GetPathaoPriceAsync(pathao.Id, input);

                z.CourierCost = Math.Round(first.FinalPrice, 2);
                z.CourierPerExtraKg = Math.Max(0, Math.Round(next.FinalPrice - first.FinalPrice, 2));
                z.PriceCheckedAt = Clock.Now;
                z.PriceError = null;
                if (z.Charge == 0) { z.Charge = z.CourierCost; z.PerExtraKg = z.CourierPerExtraKg; }

                // Pathao sends the cash fee as a fraction (0.01) on some accounts and a percent (1) on others.
                if (first.CodPercentage > 0) result.CodFeePercent = first.CodPercentage <= 1 ? first.CodPercentage * 100 : first.CodPercentage;
                result.Zones.Add(new ZonePriceResultDto { ZoneId = z.Id, Name = z.Name, Ok = true, Message = $"৳{z.CourierCost:0.##} + ৳{z.CourierPerExtraKg:0.##}/kg" });
            }
            catch (UserFriendlyException ex)
            {
                z.PriceError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                result.Zones.Add(new ZonePriceResultDto { ZoneId = z.Id, Name = z.Name, Message = ex.Message });
            }
            await _zones.UpdateAsync(z, autoSave: true);
        }

        if (result.CodFeePercent is { } cod && cod <= 20 && pathao.CodFeePercent != cod)
        {
            pathao.CodFeePercent = Math.Round(cod, 2);
            await _accounts.UpdateAsync(pathao, autoSave: true);
        }
        return result;
    }

    private async Task<CourierAccount?> PathaoAccountAsync() =>
        (await _accounts.GetListAsync(a => a.Provider == CourierProvider.Pathao && a.IsEnabled)).OrderBy(a => a.Order).FirstOrDefault();

    // ══ LISTS ════════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<ShippingItemDto> CreateItemAsync(CreateUpdateShippingItemDto input)
    {
        var item = new ShippingListItem { Kind = input.Kind };
        ApplyItem(item, input);
        if (input.Order <= 0) item.Order = (await _items.GetListAsync(i => i.Kind == input.Kind)).Select(i => i.Order).DefaultIfEmpty(0).Max() + 1;
        await _items.InsertAsync(item, autoSave: true);
        return MapItem(item);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<ShippingItemDto> UpdateItemAsync(int id, CreateUpdateShippingItemDto input)
    {
        var item = await _items.GetAsync(id);
        ApplyItem(item, input);      // the kind never changes after creation
        await _items.UpdateAsync(item, autoSave: true);
        return MapItem(item);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task DeleteItemAsync(int id) => await _items.DeleteAsync(id, autoSave: true);

    // ══ RULES & PACKAGING ════════════════════════════════════════════════════

    public async Task<RulesPageDto> GetRulesAsync()
    {
        await EnsureSeedAsync();
        var accounts = await _accounts.GetListAsync();
        var rules = (await _rules.GetListAsync()).OrderBy(r => r.Order).ThenBy(r => r.Id).ToList();

        // Replay the last 90 days of orders through the rules, so each rule shows how often it would fire.
        var since = Clock.Now.Date.AddDays(-90);
        var recent = await _orders.GetListAsync(o => o.OrderDate >= since && o.Status != OrderStatus.Cancelled);
        var pathao = accounts.FirstOrDefault(a => a.Provider == CourierProvider.Pathao);
        var facts = await _facts.BuildAsync(recent, pathao?.DefaultWeightKg ?? 1m);
        var counts = new Dictionary<int, int>();
        var unmatched = 0;
        foreach (var f in facts)
        {
            var hit = CourierRuleEngine.FirstMatch(rules, f);
            if (hit == null) unmatched++;
            else counts[hit.Id] = counts.GetValueOrDefault(hit.Id) + 1;
        }

        var items = await _items.GetListAsync();
        return new RulesPageDto
        {
            Rules = rules.Select(r => MapRule(r, accounts, counts.GetValueOrDefault(r.Id))).ToList(),
            PackingRules = ItemsOf(items, ShippingItemKind.PackingRule),
            CustomerMessages = ItemsOf(items, ShippingItemKind.CustomerMessage),
            Performance = await PerformanceAsync(accounts),
            Couriers = accounts.OrderBy(a => a.Order).Select(a => new CourierSummaryDto
            {
                Id = a.Id, Provider = a.Provider, DisplayName = a.DisplayName, ShortCode = a.ShortCode, Color = a.Color, IsEnabled = a.IsEnabled,
                Order = a.Order, ApiAvailable = CourierCatalog.HasApi(a.Provider), IsManual = CourierCatalog.IsManual(a.Provider),
            }).ToList(),
            OrdersChecked = facts.Count,
            Unmatched = unmatched,
        };
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<CourierRuleDto> CreateRuleAsync(CreateUpdateCourierRuleDto input)
    {
        await ValidateRuleAsync(input);
        var r = new CourierRule();
        ApplyRule(r, input);
        if (input.Order <= 0) r.Order = (await _rules.GetListAsync()).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
        await _rules.InsertAsync(r, autoSave: true);
        return MapRule(r, await _accounts.GetListAsync(), 0);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<CourierRuleDto> UpdateRuleAsync(int id, CreateUpdateCourierRuleDto input)
    {
        await ValidateRuleAsync(input);
        var r = await _rules.GetAsync(id);
        ApplyRule(r, input);
        await _rules.UpdateAsync(r, autoSave: true);
        return MapRule(r, await _accounts.GetListAsync(), 0);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task DeleteRuleAsync(int id) => await _rules.DeleteAsync(id, autoSave: true);

    private async Task ValidateRuleAsync(CreateUpdateCourierRuleDto input)
    {
        if (input.NoParcel) return;
        if (input.CourierAccountId == null) throw new UserFriendlyException("Choose a courier, or tick “no courier parcel”.");
        if (!await _accounts.AnyAsync(a => a.Id == input.CourierAccountId)) throw new UserFriendlyException("That courier does not exist.");
    }

    private async Task<List<CourierPerformanceDto>> PerformanceAsync(List<CourierAccount> accounts)
    {
        var since = Clock.Now.AddDays(-90);
        var shipments = await _shipments.GetListAsync(s => s.CreationTime >= since);
        return accounts.OrderBy(a => a.Order).Select(a =>
        {
            var mine = shipments.Where(s => s.CourierAccountId == a.Id).ToList();
            var stages = mine.Select(s => (s, Stage: CourierCatalog.Stage(s.Status))).ToList();
            var delivered = stages.Where(x => x.Stage == "delivered").ToList();
            var finished = stages.Count(x => x.Stage is "delivered" or "returned" or "failed");
            var days = delivered.Where(x => x.s.StatusAt.HasValue).Select(x => (x.s.StatusAt!.Value - x.s.CreationTime).TotalDays).ToList();
            return new CourierPerformanceDto
            {
                CourierAccountId = a.Id, Name = a.DisplayName, Color = a.Color, Parcels = mine.Count,
                SuccessPercent = finished == 0 ? null : (int)Math.Round(delivered.Count * 100.0 / finished),
                AverageDays = days.Count == 0 ? null : Math.Round(Math.Max(0, days.Average()), 1),
            };
        }).Where(p => p.Parcels > 0).ToList();
    }

    // ══ CASH ON DELIVERY ═════════════════════════════════════════════════════

    public async Task<CodPageDto> GetCodAsync()
    {
        await EnsureSeedAsync();
        var setting = await LoadSettingAsync();
        var accounts = (await _accounts.GetListAsync()).ToDictionary(a => a.Id);
        var shipments = await _shipments.GetListAsync();
        var payouts = (await _payouts.GetListAsync()).OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).ToList();
        var today = Clock.Now.Date;
        var orderIds = shipments.Select(s => s.OrderId).Distinct().ToList();
        var orderNumbers = (await _orders.GetListAsync(o => orderIds.Contains(o.Id)))
            .ToDictionary(o => o.Id, o => o.OrderNumber ?? $"#{o.Id}");

        var unpaid = shipments
            .Where(s => s.PayoutId == null && s.CodAmount > 0 && CourierCatalog.Stage(s.Status) == "delivered" && accounts.ContainsKey(s.CourierAccountId))
            .Select(s => ToParcel(s, accounts[s.CourierAccountId], orderNumbers))
            .OrderBy(p => p.DeliveredAt).ToList();

        var couriers = unpaid.GroupBy(p => p.CourierAccountId).Select(g =>
        {
            var a = accounts[g.Key];
            return new CodCourierDto
            {
                CourierAccountId = a.Id, Name = a.DisplayName, ShortCode = a.ShortCode, Color = a.Color, Provider = a.Provider, Parcels = g.Count(),
                Collected = g.Sum(p => p.Cod), Fee = g.Sum(p => p.Fee), ShouldReceive = g.Sum(p => p.Expected),
                Schedule = a.PayoutSchedule, CodFeePercent = a.CodFeePercent,
            };
        }).OrderByDescending(c => c.Collected).ToList();

        var parcelCounts = shipments.Where(s => s.PayoutId.HasValue).GroupBy(s => s.PayoutId!.Value).ToDictionary(g => g.Key, g => g.Count());
        var payoutDtos = payouts.Take(50).Select(p => MapPayout(p, accounts, parcelCounts.GetValueOrDefault(p.Id))).ToList();
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var thisMonth = payoutDtos.Where(p => p.Date >= monthStart).ToList();
        var shorts = payoutDtos.Where(p => p.Status == "short").ToList();

        var issues = new List<CodIssueDto>();
        foreach (var p in shorts.Take(5))
            issues.Add(new CodIssueDto
            {
                Tone = "red", Title = $"{p.CourierName} payout short by ৳{-p.Difference:#,##0.##}",
                Text = $"Payout of {p.Date:d MMM} for {p.Parcels} parcels. Raise it with {p.CourierName}.",
                Action = "view-payout", ActionLabel = "Open", PayoutId = p.Id, CourierAccountId = p.CourierAccountId,
            });

        var lateCutoff = today.AddDays(-setting.NoCashAlertDays);
        foreach (var g in unpaid.Where(p => p.DeliveredAt.Date <= lateCutoff && !CourierCatalog.IsManual(accounts[p.CourierAccountId].Provider)).GroupBy(p => p.CourierAccountId))
            issues.Add(new CodIssueDto
            {
                Tone = "amber", Title = $"{g.Count()} {(g.Count() == 1 ? "parcel" : "parcels")} delivered, no cash yet",
                Text = $"{accounts[g.Key].DisplayName} delivered them {setting.NoCashAlertDays}+ days ago. Check whether the payout came in.",
                Action = "payout", ActionLabel = "Record payout", CourierAccountId = g.Key,
            });

        foreach (var g in unpaid.Where(p => CourierCatalog.IsManual(accounts[p.CourierAccountId].Provider)).GroupBy(p => p.CourierAccountId))
            issues.Add(new CodIssueDto
            {
                Tone = "grey", Title = $"{accounts[g.Key].DisplayName} cash not banked",
                Text = $"৳{g.Sum(p => p.Cod):#,##0.##} collected on {g.Count()} {(g.Count() == 1 ? "delivery" : "deliveries")} is still with the team.",
                Action = "payout", ActionLabel = "Bank it", CourierAccountId = g.Key,
            });

        var finance = (await _financeAccounts.GetListAsync(a => a.IsActive)).OrderBy(a => a.Order).ThenBy(a => a.Id)
            .Select(a => new CodAccountOptionDto { Id = a.Id, Name = a.Name }).ToList();

        return new CodPageDto
        {
            Holding = unpaid.Sum(p => p.Expected), HoldingParcels = unpaid.Count,
            OldestUnpaidDays = unpaid.Count == 0 ? null : (today - unpaid.Min(p => p.DeliveredAt).Date).Days,
            ReceivedThisMonth = thisMonth.Sum(p => p.Amount), ReceivedCount = thisMonth.Count, MatchedCount = thisMonth.Count(p => p.Status == "matched"),
            ShortTotal = shorts.Sum(p => -p.Difference), ShortCount = shorts.Count,
            Couriers = couriers, Payouts = payoutDtos, Issues = issues, Unpaid = unpaid, FinanceAccounts = finance,
        };
    }

    /// <summary>
    /// Records cash a courier paid out and marks the chosen delivered parcels as paid. The expected amount is
    /// frozen so a later fee change does not rewrite history. Optionally books it as money in on Financials.
    /// </summary>
    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<CodPayoutDto> CreatePayoutAsync(CreateCourierPayoutDto input)
    {
        var account = await _accounts.GetAsync(input.CourierAccountId);
        var ids = input.ShipmentIds.Distinct().ToList();
        var shipments = await _shipments.GetListAsync(s => ids.Contains(s.Id));
        if (shipments.Count != ids.Count) throw new UserFriendlyException("Some of the chosen parcels no longer exist.");
        if (shipments.Any(s => s.CourierAccountId != account.Id)) throw new UserFriendlyException($"Every parcel in the payout must belong to {account.DisplayName}.");
        if (shipments.Any(s => s.PayoutId != null)) throw new UserFriendlyException("Some of the chosen parcels are already in another payout.");
        if (shipments.Any(s => CourierCatalog.Stage(s.Status) != "delivered")) throw new UserFriendlyException("Only delivered parcels can be paid out.");

        var expected = shipments.Sum(s => ExpectedOf(s, account));
        var payout = new CourierPayout
        {
            CourierAccountId = account.Id, Date = input.Date.Date, Amount = Math.Round(input.Amount, 2), Expected = expected,
            Reference = Clean(input.Reference), Note = Clean(input.Note),
        };
        await _payouts.InsertAsync(payout, autoSave: true);

        foreach (var s in shipments) s.PayoutId = payout.Id;
        await _shipments.UpdateManyAsync(shipments, autoSave: true);

        if (input.FinanceAccountId.HasValue && payout.Amount > 0)
        {
            var fa = await _financeAccounts.FirstOrDefaultAsync(a => a.Id == input.FinanceAccountId.Value && a.IsActive)
                     ?? throw new UserFriendlyException("Choose an active Financials account.");
            var tx = await _financeTxs.InsertAsync(new FinanceTransaction
            {
                Date = payout.Date, AccountId = fa.Id, Direction = FinanceDirection.In, Amount = payout.Amount,
                Category = "Courier cash payouts", Description = $"{account.DisplayName} payout · {shipments.Count} parcels",
                Reference = payout.Reference, Source = FinanceTxSource.Manual,
            }, autoSave: true);
            payout.FinanceTransactionId = tx.Id;
            await _payouts.UpdateAsync(payout, autoSave: true);
        }

        return MapPayout(payout, new Dictionary<int, CourierAccount> { [account.Id] = account }, shipments.Count);
    }

    /// <summary>Undoes a payout: its parcels go back to "courier is holding" and any Financials row is removed.</summary>
    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task DeletePayoutAsync(int id)
    {
        var payout = await _payouts.GetAsync(id);
        var shipments = await _shipments.GetListAsync(s => s.PayoutId == id);
        foreach (var s in shipments) s.PayoutId = null;
        await _shipments.UpdateManyAsync(shipments, autoSave: true);
        if (payout.FinanceTransactionId.HasValue)
            await _financeTxs.DeleteAsync(t => t.Id == payout.FinanceTransactionId.Value, autoSave: true);
        await _payouts.DeleteAsync(payout, autoSave: true);
    }

    // ══ MAPPING & HELPERS ════════════════════════════════════════════════════

    /// <summary>What the courier owes us for one delivered parcel: the cash minus its delivery fee and its cut.</summary>
    private static decimal ExpectedOf(Shipment s, CourierAccount a) =>
        Math.Round(s.CodAmount - s.DeliveryFee - s.CodAmount * a.CodFeePercent / 100m, 2);

    private static CodParcelDto ToParcel(Shipment s, CourierAccount a, Dictionary<int, string> numbers) => new()
    {
        ShipmentId = s.Id, CourierAccountId = s.CourierAccountId, ConsignmentId = s.ConsignmentId,
        OrderNumber = numbers.GetValueOrDefault(s.OrderId, $"#{s.OrderId}"), DeliveredAt = s.StatusAt ?? s.CreationTime,
        Cod = s.CodAmount, Fee = Math.Round(s.DeliveryFee + s.CodAmount * a.CodFeePercent / 100m, 2), Expected = ExpectedOf(s, a),
    };

    private static CodPayoutDto MapPayout(CourierPayout p, Dictionary<int, CourierAccount> accounts, int parcels)
    {
        var diff = p.Amount - p.Expected;
        return new CodPayoutDto
        {
            Id = p.Id, CourierAccountId = p.CourierAccountId, CourierName = accounts.TryGetValue(p.CourierAccountId, out var a) ? a.DisplayName : "Courier",
            Date = p.Date, Amount = p.Amount, Expected = p.Expected, Difference = diff, Parcels = parcels,
            Status = Math.Abs(diff) < Eps ? "matched" : diff < 0 ? "short" : "over",
            Reference = p.Reference, Note = p.Note, Banked = p.FinanceTransactionId.HasValue,
        };
    }

    private CourierRuleDto MapRule(CourierRule r, List<CourierAccount> accounts, int count)
    {
        var courier = accounts.FirstOrDefault(a => a.Id == r.CourierAccountId);
        return new CourierRuleDto
        {
            Id = r.Id, Order = r.Order, IsEnabled = r.IsEnabled, MatchAny = r.MatchAny, ProductKeyword = r.ProductKeyword,
            AnyItemOverKg = r.AnyItemOverKg, TotalWeightUnderKg = r.TotalWeightUnderKg, AddressContains = r.AddressContains,
            CodOver = r.CodOver, NeedsInstallation = r.NeedsInstallation, CourierAccountId = r.CourierAccountId, NoParcel = r.NoParcel,
            ThenNote = r.ThenNote, IfText = CourierRuleEngine.DescribeIf(r), ThenText = CourierRuleEngine.DescribeThen(r, courier?.DisplayName),
            CourierColor = courier?.Color, MatchCount = count,
        };
    }

    private static void ApplyRule(CourierRule r, CreateUpdateCourierRuleDto i)
    {
        r.IsEnabled = i.IsEnabled; r.MatchAny = i.MatchAny; r.ProductKeyword = Clean(i.ProductKeyword); r.AnyItemOverKg = i.AnyItemOverKg;
        r.TotalWeightUnderKg = i.TotalWeightUnderKg; r.AddressContains = Clean(i.AddressContains); r.CodOver = i.CodOver;
        r.NeedsInstallation = i.NeedsInstallation; r.NoParcel = i.NoParcel; r.CourierAccountId = i.NoParcel ? null : i.CourierAccountId;
        r.ThenNote = Clean(i.ThenNote);
        if (i.Order > 0) r.Order = i.Order;
    }

    private static void ApplyZone(ShippingZone z, CreateUpdateShippingZoneDto i)
    {
        z.Name = i.Name.Trim(); z.Note = Clean(i.Note); z.Charge = Math.Round(i.Charge, 2); z.PerExtraKg = Math.Round(i.PerExtraKg, 2);
        z.CourierCost = Math.Round(i.CourierCost, 2); z.Days = Clean(i.Days);
        if (z.PathaoZoneId != i.PathaoZoneId) { z.PriceCheckedAt = null; z.PriceError = null; }   // new location: old price no longer applies
        z.PathaoCityId = i.PathaoZoneId == null ? null : i.PathaoCityId; z.PathaoCityName = i.PathaoZoneId == null ? null : Clean(i.PathaoCityName);
        z.PathaoZoneId = i.PathaoZoneId; z.PathaoZoneName = i.PathaoZoneId == null ? null : Clean(i.PathaoZoneName);
        if (i.Order > 0) z.Order = i.Order;
    }

    private static void ApplyItem(ShippingListItem item, CreateUpdateShippingItemDto i)
    {
        item.Title = i.Title.Trim(); item.Detail = Clean(i.Detail); item.Extra = Clean(i.Extra); item.Color = Clean(i.Color); item.Flag = i.Flag;
        if (i.Order > 0) item.Order = i.Order;
    }

    private static ShippingZoneDto MapZone(ShippingZone z) => new()
    {
        Id = z.Id, Name = z.Name, Note = z.Note, Charge = z.Charge, PerExtraKg = z.PerExtraKg, CourierCost = z.CourierCost,
        Days = z.Days, Order = z.Order, Margin = z.Charge - z.CourierCost,
        PathaoCityId = z.PathaoCityId, PathaoCityName = z.PathaoCityName, PathaoZoneId = z.PathaoZoneId, PathaoZoneName = z.PathaoZoneName,
        CourierPerExtraKg = z.CourierPerExtraKg, PriceCheckedAt = z.PriceCheckedAt, PriceError = z.PriceError,
    };

    private static ShippingItemDto MapItem(ShippingListItem i) => new()
    {
        Id = i.Id, Kind = i.Kind, Title = i.Title, Detail = i.Detail, Extra = i.Extra, Color = i.Color, Flag = i.Flag, Order = i.Order,
    };

    private static List<ShippingItemDto> ItemsOf(List<ShippingListItem> items, ShippingItemKind kind) =>
        items.Where(i => i.Kind == kind).OrderBy(i => i.Order).ThenBy(i => i.Id).Select(MapItem).ToList();

    private static ShippingSettingDto MapSetting(ShippingSetting s) => new()
    {
        FreeDeliveryEnabled = s.FreeDeliveryEnabled, FreeDeliveryOver = s.FreeDeliveryOver, WeightChargeEnabled = s.WeightChargeEnabled,
        WeightIncludedKg = s.WeightIncludedKg, CodFeePassed = s.CodFeePassed, CodFeePercent = s.CodFeePercent,
        CustomerChoosesCourier = s.CustomerChoosesCourier, OwnTruckPerKm = s.OwnTruckPerKm, MinTripCharge = s.MinTripCharge,
        NoCashAlertDays = s.NoCashAlertDays,
    };

    private async Task<ShippingSetting> LoadSettingAsync() => (await _settings.GetListAsync()).OrderBy(s => s.Id).First();

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ══ DEFAULTS ═════════════════════════════════════════════════════════════

    /// <summary>Starter content, created once. Prices are only a starting point — check them before relying on them.</summary>
    private async Task EnsureSeedAsync()
    {
        if (await _settings.GetCountAsync() > 0) return;
        await SeedLock.WaitAsync();
        try
        {
            if (await _settings.GetCountAsync() > 0) return;
            await _settings.InsertAsync(new ShippingSetting(), autoSave: true);

            // No starting zones: each one is added on the page and priced from Pathao.

            if (await _items.GetCountAsync() == 0)
            {
                var kinds = new Dictionary<ShippingItemKind, int>();
                ShippingListItem Item(ShippingItemKind k, string title, string detail, string extra, string color, bool flag = true)
                {
                    kinds[k] = kinds.GetValueOrDefault(k) + 1;
                    return new ShippingListItem { Kind = k, Title = title, Detail = detail, Extra = extra, Color = color, Flag = flag, Order = kinds[k] };
                }
                await _items.InsertManyAsync(new[]
                {
                    Item(ShippingItemKind.BigItem, "Solar panels", "Large panels are too long and heavy for most couriers.", "Own truck", "#0E6B3F"),
                    Item(ShippingItemKind.BigItem, "Lead-acid batteries", "Over 20 kg and classed as dangerous goods.", "Own truck", "#0E6B3F"),
                    Item(ShippingItemKind.BigItem, "Lithium batteries", "Couriers need a declaration; air transport is not allowed.", "Own truck or surface only", "#D97706"),
                    Item(ShippingItemKind.BigItem, "Full packages", "Panels, inverter, battery and frame on one order.", "Own truck + installer", "#0E6B3F"),

                    Item(ShippingItemKind.ReturnPolicy, "Customer refused or was not there", "Return charge is ours. The order goes back to stock and the product-return rule applies.", "We pay", "#D97706"),
                    Item(ShippingItemKind.ReturnPolicy, "Wrong or damaged item sent", "Our mistake — we pay both ways and send the replacement free.", "We pay", "#B42318"),
                    Item(ShippingItemKind.ReturnPolicy, "Customer changed their mind after delivery", "Within 7 days. The restocking fee and the return courier charge come off the refund.", "Customer pays", "#5F6B63"),

                    Item(ShippingItemKind.PackingRule, "Solar panel", "Original carton, corner guards, standing upright on the truck bed. Never laid flat under anything.", "Own truck", "#D97706"),
                    Item(ShippingItemKind.PackingRule, "Battery", "Terminals taped, upright, strapped. Lead-acid never goes by air.", "Own truck", "#D97706"),
                    Item(ShippingItemKind.PackingRule, "Inverter or IPS", "Double box with 5 cm of foam. Mark Fragile on all six sides.", "Courier", "#2563EB"),
                    Item(ShippingItemKind.PackingRule, "Cable and connectors", "Coil and tie, poly bag, then a box. Count the connectors on the slip.", "Courier", "#2563EB"),
                    Item(ShippingItemKind.PackingRule, "Small items and accessories", "One box per order. Put the serial list and the warranty card inside.", "Courier", "#0E6B3F"),

                    Item(ShippingItemKind.CustomerMessage, "Parcel created", "SMS with the consignment number and a tracking link.", "SMS", "#1E40AF"),
                    Item(ShippingItemKind.CustomerMessage, "Out for delivery", "SMS with the rider's phone number, sent by the courier.", "Courier", "#5F6B63"),
                    Item(ShippingItemKind.CustomerMessage, "Delivered", "SMS thanking them, with a link to rate the delivery.", "SMS", "#1E40AF"),
                    Item(ShippingItemKind.CustomerMessage, "Delivery failed", "Call from our team the same day, not an SMS.", "Call", "#D97706"),
                }, autoSave: true);
            }

            if (await _rules.GetCountAsync() == 0)
            {
                var accounts = await _accounts.GetListAsync();
                int? Id(CourierProvider p) => accounts.Where(a => a.Provider == p).OrderBy(a => a.Order).FirstOrDefault()?.Id;
                var pathao = Id(CourierProvider.Pathao);
                var own = Id(CourierProvider.OwnDelivery);
                var steadfast = Id(CourierProvider.Steadfast);
                if (pathao != null && own != null)
                {
                    var o = 0;
                    await _rules.InsertManyAsync(new[]
                    {
                        new CourierRule { Order = ++o, MatchAny = true, ProductKeyword = "panel", AnyItemOverKg = 20, CourierAccountId = own, ThenNote = "book a slot with the installation team" },
                        new CourierRule { Order = ++o, NeedsInstallation = true, NoParcel = true, ThenNote = "Project planning handles delivery" },
                        new CourierRule { Order = ++o, AddressContains = "Chattogram, Chittagong", TotalWeightUnderKg = 10, CourierAccountId = pathao, ThenNote = "on demand from the nearest warehouse" },
                        // Starts off: Steadfast's API is not built into the app yet.
                        new CourierRule { Order = ++o, IsEnabled = false, CodOver = 20000, CourierAccountId = steadfast ?? pathao, ThenNote = "call the customer to confirm before pickup" },
                        new CourierRule { Order = ++o, TotalWeightUnderKg = 10, CourierAccountId = pathao, ThenNote = "normal delivery from the nearest warehouse" },
                    }, autoSave: true);
                }
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }
}
