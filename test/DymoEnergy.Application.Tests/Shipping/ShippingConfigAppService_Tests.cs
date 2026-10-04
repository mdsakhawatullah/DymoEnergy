using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DymoEnergy.Finance;
using DymoEnergy.Orders;
using DymoEnergy.Products;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Shipping;

public abstract class ShippingConfigAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IShippingConfigAppService _config;
    private readonly IShippingAppService _shipping;

    protected ShippingConfigAppService_Tests()
    {
        _config = GetRequiredService<IShippingConfigAppService>();
        _shipping = GetRequiredService<IShippingAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Transient<PathaoClient, FakePathaoClient>());
    }

    /// <summary>Pathao answering the price plan: ৳110 for the first kg, ৳135 for two.</summary>
    private static (HttpStatusCode, string) PathaoPrices(string method, string path)
    {
        if (path.EndsWith("/issue-token"))
            return (HttpStatusCode.OK, """{"token_type":"Bearer","expires_in":432000,"access_token":"ACCESS-1","refresh_token":"REFRESH-1"}""");
        if (path.EndsWith("/merchant/price-plan"))
        {
            var n = FakePathaoServer.Requests.Count(r => r.Path.EndsWith("/merchant/price-plan"));
            var price = n % 2 == 1 ? 110 : 135;
            return (HttpStatusCode.OK, "{\"type\":\"success\",\"code\":200,\"data\":{\"price\":" + price + ",\"discount\":0,\"promo_discount\":0,\"plan_id\":69,\"cod_enabled\":1,\"cod_percentage\":0.01,\"additional_charge\":0,\"final_price\":" + price + "}}");
        }
        return (HttpStatusCode.NotFound, """{"message":"Not found","type":"error","code":404}""");
    }

    private async Task<int> ConnectPathaoAsync()
    {
        var overview = await _shipping.GetOverviewAsync();
        var pathao = await _shipping.GetCourierAsync(overview.Couriers.First(c => c.Provider == CourierProvider.Pathao).Id);
        foreach (var (k, v) in new[] { ("base_url", "https://sandbox.example.test/"), ("client_id", "CLIENT-ID-1234"), ("client_secret", "SECRET-abcd"), ("username", "ops@example.test"), ("password", "pass-wxyz") })
            await _shipping.UpdateCourierCredentialAsync(pathao.Id, new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = k, Value = v });
        await _shipping.UpdateCourierSettingsAsync(pathao.Id, new UpdateCourierSettingsDto
        {
            DisplayName = pathao.DisplayName, ShortCode = pathao.ShortCode, Color = pathao.Color, IsEnabled = true,
            PickupStoreId = "5501", PickupStoreName = "Agrabad warehouse", DefaultDeliveryType = 48, DefaultItemType = 2, DefaultWeightKg = 1,
        });
        return pathao.Id;
    }

    private async Task<int> AddOrderAsync(string number, string product, string weight, string address, double due = 0)
    {
        var id = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var products = GetRequiredService<IRepository<Product, int>>();
            var orders = GetRequiredService<IRepository<Order, int>>();
            var items = GetRequiredService<IRepository<OrderItem, int>>();
            var p = await products.InsertAsync(new Product { Name = product, Weight = weight, Price = 100 }, autoSave: true);
            var o = await orders.InsertAsync(new Order
            {
                OrderNumber = number, OrderDate = DateTime.Now, Status = OrderStatus.Confirmed, CustomerName = "Rafiq Islam",
                DeliveryPhone = "01812345678", DeliveryAddress = address, GrandTotal = due, BalanceDue = due,
            }, autoSave: true);
            await items.InsertAsync(new OrderItem { OrderId = o.Id, ProductId = p.Id, ProductName = p.Name, Quantity = 1, UnitPrice = 100 }, autoSave: true);
            id = o.Id;
        });
        return id;
    }

    private async Task<int> AddDeliveredShipmentAsync(int courierId, decimal cod, decimal fee, int daysAgo)
    {
        var orderId = await AddOrderAsync("ORD-C-" + Guid.NewGuid().ToString("N")[..6], "Inverter", "8 kg", "Agrabad, Chattogram");
        var id = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var s = await GetRequiredService<IRepository<Shipment, int>>().InsertAsync(new Shipment
            {
                OrderId = orderId, CourierAccountId = courierId, Environment = CourierEnvironment.Live, ConsignmentId = "DF" + orderId,
                Status = "Delivered", StatusAt = DateTime.Now.AddDays(-daysAgo), CodAmount = cod, DeliveryFee = fee,
                RecipientName = "Rafiq Islam", RecipientPhone = "01812345678", RecipientAddress = "Agrabad, Chattogram",
            }, autoSave: true);
            id = s.Id;
        });
        return id;
    }

    // ── charges & zones ───────────────────────────────────────────────────

    [Fact]
    public async Task Should_Price_Zones_From_Pathao_Instead_Of_Made_Up_Numbers()
    {
        var page = await _config.GetChargesAsync();
        page.Zones.ShouldBeEmpty();                       // nothing invented
        page.ReturnPolicies.Count.ShouldBe(3);

        FakePathaoServer.Reset(PathaoPrices);
        var pathaoId = await ConnectPathaoAsync();
        (await _config.GetChargesAsync()).PathaoAccountId.ShouldBe(pathaoId);

        var dhaka = await _config.CreateZoneAsync(new CreateUpdateShippingZoneDto
        {
            Name = "Inside Dhaka", Days = "1–2 days", PathaoCityId = 1, PathaoCityName = "Dhaka", PathaoZoneId = 298, PathaoZoneName = "Mirpur",
        });
        var unlinked = await _config.CreateZoneAsync(new CreateUpdateShippingZoneDto { Name = "Hill districts", Charge = 250 });

        var result = await _config.RefreshZonePricesAsync(new RefreshZonePricesInput());
        result.Zones.Single(z => z.ZoneId == dhaka.Id).Ok.ShouldBeTrue();
        result.Zones.Single(z => z.ZoneId == unlinked.Id).Ok.ShouldBeFalse();
        result.CodFeePercent.ShouldBe(1);

        var zones = (await _config.GetChargesAsync()).Zones;
        var priced = zones.Single(z => z.Id == dhaka.Id);
        priced.CourierCost.ShouldBe(110);
        priced.CourierPerExtraKg.ShouldBe(25);
        priced.Charge.ShouldBe(110);                       // no customer price yet: starts at Pathao's
        priced.PriceCheckedAt.ShouldNotBeNull();
        zones.Single(z => z.Id == unlinked.Id).CourierCost.ShouldBe(0);
        (await _shipping.GetCourierAsync(pathaoId)).CodFeePercent.ShouldBe(1);

        // The body Pathao got says which location and weight were priced.
        var body = FakePathaoServer.Requests.First(r => r.Path.EndsWith("/merchant/price-plan")).Body;
        body.ShouldContain("\"recipient_zone\":298");
        body.ShouldContain("\"store_id\":\"5501\"");

        // A customer price set by hand is kept on the next refresh.
        await _config.UpdateZoneAsync(dhaka.Id, new CreateUpdateShippingZoneDto
        {
            Name = "Inside Dhaka", Charge = 130, PerExtraKg = 25, CourierCost = 110, PathaoCityId = 1, PathaoCityName = "Dhaka", PathaoZoneId = 298, PathaoZoneName = "Mirpur",
        });
        await _config.RefreshZonePricesAsync(new RefreshZonePricesInput { ZoneId = dhaka.Id });
        var again = (await _config.GetChargesAsync()).Zones.Single(z => z.Id == dhaka.Id);
        again.Charge.ShouldBe(130);
        again.Margin.ShouldBe(20);

        await _config.DeleteZoneAsync(unlinked.Id);
        (await _config.GetChargesAsync()).Zones.Count.ShouldBe(1);
    }

    // ── rules ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Pick_The_Courier_From_The_First_Matching_Rule()
    {
        var couriers = (await _shipping.GetOverviewAsync()).Couriers;
        var pathao = couriers.First(c => c.Provider == CourierProvider.Pathao);
        var own = couriers.First(c => c.Provider == CourierProvider.OwnDelivery);

        var panel = await AddOrderAsync("ORD-R-1", "Solar panel 550W", "28 kg", "Mirpur 10, Dhaka");
        var local = await AddOrderAsync("ORD-R-2", "Charge controller", "2 kg", "Agrabad, Chattogram");
        var dhaka = await AddOrderAsync("ORD-R-3", "Charge controller", "3 kg", "Mirpur 10, Dhaka");

        var rules = await _config.GetRulesAsync();
        rules.Rules.Count.ShouldBe(5);
        rules.Rules.Single(r => r.CodOver == 20000).IsEnabled.ShouldBeFalse();
        rules.OrdersChecked.ShouldBe(3);
        rules.Rules[0].MatchCount.ShouldBe(1);
        rules.Rules[0].IfText.ShouldNotBeNullOrWhiteSpace();
        rules.Rules[2].MatchCount.ShouldBe(1);
        rules.Rules[4].MatchCount.ShouldBe(1);

        var ready = (await _shipping.GetShipmentsAsync(new GetShipmentsInput())).Ready;
        ready.Single(r => r.OrderId == panel).SuggestedCourierId.ShouldBe(own.Id);
        ready.Single(r => r.OrderId == panel).RuleNumber.ShouldBe(1);
        ready.Single(r => r.OrderId == local).SuggestedCourierId.ShouldBe(pathao.Id);
        ready.Single(r => r.OrderId == dhaka).SuggestedCourierId.ShouldBe(pathao.Id);

        // A rule must say where the parcel goes, unless it says there is no parcel.
        await Should.ThrowAsync<UserFriendlyException>(() => _config.CreateRuleAsync(new CreateUpdateCourierRuleDto { ProductKeyword = "cable" }));
        var noParcel = await _config.CreateRuleAsync(new CreateUpdateCourierRuleDto { ProductKeyword = "cable", NoParcel = true, CourierAccountId = pathao.Id });
        noParcel.CourierAccountId.ShouldBeNull();
        noParcel.Order.ShouldBe(6);
    }

    // ── cash on delivery ──────────────────────────────────────────────────

    [Fact]
    public async Task Should_Match_A_Short_Payout_Book_It_In_Financials_And_Undo_It()
    {
        var pathao = await _shipping.GetCourierAsync((await _shipping.GetOverviewAsync()).Couriers.First(c => c.Provider == CourierProvider.Pathao).Id);
        await _shipping.UpdateCourierSettingsAsync(pathao.Id, new UpdateCourierSettingsDto
        {
            DisplayName = pathao.DisplayName, ShortCode = pathao.ShortCode, Color = pathao.Color, IsEnabled = true,
            CodFeePercent = 1, PayoutSchedule = "Every Tuesday",
        });

        var a = await AddDeliveredShipmentAsync(pathao.Id, 10000, 100, 6);   // owed 10000 − 100 − 100 = 9800
        var b = await AddDeliveredShipmentAsync(pathao.Id, 5000, 80, 1);     // owed 5000 − 80 − 50 = 4870

        var bankId = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var bank = await GetRequiredService<IRepository<FinanceAccount, int>>()
                .InsertAsync(new FinanceAccount { Name = "City Bank", ShortCode = "CB", OpeningDate = DateTime.Today.AddYears(-1) }, autoSave: true);
            bankId = bank.Id;
        });

        var cod = await _config.GetCodAsync();
        cod.Holding.ShouldBe(14670);
        cod.HoldingParcels.ShouldBe(2);
        cod.OldestUnpaidDays.ShouldBe(6);
        cod.Couriers.Single().Schedule.ShouldBe("Every Tuesday");
        cod.Issues.ShouldContain(i => i.Action == "payout" && i.CourierAccountId == pathao.Id);   // a is past the 4-day alert
        cod.FinanceAccounts.ShouldContain(f => f.Id == bankId);

        var payout = await _config.CreatePayoutAsync(new CreateCourierPayoutDto
        {
            CourierAccountId = pathao.Id, Date = DateTime.Today, Amount = 14600, Reference = "PTH-1",
            ShipmentIds = new() { a, b }, FinanceAccountId = bankId,
        });
        payout.Expected.ShouldBe(14670);
        payout.Status.ShouldBe("short");
        payout.Difference.ShouldBe(-70);
        payout.Banked.ShouldBeTrue();

        cod = await _config.GetCodAsync();
        cod.Holding.ShouldBe(0);
        cod.ShortTotal.ShouldBe(70);
        cod.Issues.ShouldContain(i => i.Tone == "red" && i.PayoutId == payout.Id);

        await WithUnitOfWorkAsync(async () =>
        {
            var tx = await GetRequiredService<IRepository<FinanceTransaction, int>>().GetListAsync(t => t.AccountId == bankId);
            tx.Single().Amount.ShouldBe(14600);
            tx.Single().Direction.ShouldBe(FinanceDirection.In);
        });

        // A parcel can only be paid once.
        await Should.ThrowAsync<UserFriendlyException>(() => _config.CreatePayoutAsync(new CreateCourierPayoutDto
        {
            CourierAccountId = pathao.Id, Date = DateTime.Today, Amount = 1, ShipmentIds = new() { a },
        }));

        await _config.DeletePayoutAsync(payout.Id);
        (await _config.GetCodAsync()).Holding.ShouldBe(14670);
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<IRepository<FinanceTransaction, int>>().GetListAsync(t => t.AccountId == bankId)).ShouldBeEmpty());
    }
}
