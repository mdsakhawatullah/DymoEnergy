using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Orders;
using DymoEnergy.Products;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Shipping;

/// <summary>Stands in for Pathao's servers: records every request and answers from a script.</summary>
public class FakePathaoServer : HttpMessageHandler
{
    public static readonly ConcurrentQueue<(string Method, string Path, string? Auth, string Body)> Requests = new();
    public static Func<string, string, (HttpStatusCode, string)> Answer = (_, _) => (HttpStatusCode.NotFound, "{}");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
        Requests.Enqueue((request.Method.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.Parameter, body));
        var (status, json) = Answer(request.Method.Method, request.RequestUri.AbsolutePath);
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    public static void Reset(Func<string, string, (HttpStatusCode, string)> answer)
    {
        Requests.Clear();
        Answer = answer;
    }
}

public class FakePathaoClient : PathaoClient
{
    private static readonly HttpClient Client = new(new FakePathaoServer());
    protected override HttpClient Http => Client;
}

public abstract class ShippingAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IShippingAppService _service;

    protected ShippingAppService_Tests()
    {
        _service = GetRequiredService<IShippingAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Transient<PathaoClient, FakePathaoClient>());
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static (HttpStatusCode, string) PathaoHappy(string method, string path) => path switch
    {
        "/aladdin/api/v1/issue-token" => (HttpStatusCode.OK, """{"token_type":"Bearer","expires_in":432000,"access_token":"ACCESS-1","refresh_token":"REFRESH-1"}"""),
        "/aladdin/api/v1/stores" when method == "GET" => (HttpStatusCode.OK, """{"message":"Store list fetched.","type":"success","code":200,"data":{"data":[{"store_id":"5501","store_name":"Agrabad warehouse","store_address":"House 1, Agrabad, Chattogram","is_active":1,"city_id":"2","zone_id":"10","hub_id":"3","is_default_store":true}],"total":1}}"""),
        "/aladdin/api/v1/city-list" => (HttpStatusCode.OK, """{"type":"success","code":200,"data":{"data":[{"city_id":1,"city_name":"Dhaka"},{"city_id":2,"city_name":"Chittagong"}]}}"""),
        "/aladdin/api/v1/orders" => (HttpStatusCode.OK, """{"message":"Order Created Successfully","type":"success","code":200,"data":{"consignment_id":"DF2609XYZ","merchant_order_id":"ORD-T-1","order_status":"Pending","delivery_fee":80}}"""),
        _ when path.EndsWith("/info") => (HttpStatusCode.OK, """{"type":"success","code":200,"data":{"consignment_id":"DF2609XYZ","order_status":"Delivered","order_status_slug":"Delivered","updated_at":"2026-10-03 11:00:00"}}"""),
        _ => (HttpStatusCode.NotFound, """{"message":"Not found","type":"error","code":404}"""),
    };

    private async Task<CourierDetailDto> PathaoAsync()
    {
        var overview = await _service.GetOverviewAsync();
        return await _service.GetCourierAsync(overview.Couriers.First(c => c.Provider == CourierProvider.Pathao).Id);
    }

    private async Task SaveSandboxKeysAsync(int id)
    {
        foreach (var (k, v) in new[] { ("base_url", "https://sandbox.example.test/"), ("client_id", "CLIENT-ID-1234"), ("client_secret", "SECRET-abcd"), ("username", "ops@example.test"), ("password", "pass-wxyz") })
            await _service.UpdateCourierCredentialAsync(id, new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = k, Value = v });
    }

    private async Task<int> AddOrderAsync(string number, string phone, string address, double due, string? productWeight = "2 kg", double qty = 1)
    {
        var id = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var products = GetRequiredService<IRepository<Product, int>>();
            var orders = GetRequiredService<IRepository<Order, int>>();
            var items = GetRequiredService<IRepository<OrderItem, int>>();
            var p = await products.InsertAsync(new Product { Name = "Battery " + number, Weight = productWeight, Price = 100 }, autoSave: true);
            var o = await orders.InsertAsync(new Order
            {
                OrderNumber = number, OrderDate = DateTime.Now, Status = OrderStatus.Confirmed, CustomerName = "Rafiq Islam",
                DeliveryPhone = phone, DeliveryAddress = address, GrandTotal = due, BalanceDue = due,
            }, autoSave: true);
            await items.InsertAsync(new OrderItem { OrderId = o.Id, ProductId = p.Id, ProductName = p.Name, Quantity = qty, UnitPrice = 100 }, autoSave: true);
            id = o.Id;
        });
        return id;
    }

    // ── couriers & keys ───────────────────────────────────────────────────

    [Fact]
    public async Task Should_Seed_Couriers_With_Unbuilt_Apis_Switched_Off()
    {
        var o = await _service.GetOverviewAsync();
        o.Couriers.Select(c => c.DisplayName).ShouldBe(new[] { "Pathao Courier", "Steadfast Courier", "RedX", "eCourier", "Own delivery team", "Customer pickup" });
        o.Couriers.First(c => c.Provider == CourierProvider.Pathao).Status.ShouldBe("not-connected");
        o.Couriers.First(c => c.Provider == CourierProvider.RedX).Status.ShouldBe("off");
        o.Couriers.Count(c => c.Status == "always").ShouldBe(2);
        o.ConnectedCount.ShouldBe(2);
        (await _service.GetOverviewAsync()).Couriers.Count.ShouldBe(6);
    }

    [Fact]
    public async Task Should_Store_Keys_Encrypted_Mask_Secrets_And_Log_Reveals()
    {
        var pathao = await PathaoAsync();
        await SaveSandboxKeysAsync(pathao.Id);

        // Nothing is stored as plain text.
        await WithUnitOfWorkAsync(async () =>
        {
            var rows = await GetRequiredService<IRepository<CourierCredential, int>>().GetListAsync();
            rows.ShouldNotBeEmpty();
            rows.ShouldAllBe(r => !r.EncryptedValue.Contains("SECRET-abcd") && !r.EncryptedValue.Contains("pass-wxyz") && !r.EncryptedValue.Contains("CLIENT-ID"));
        });

        var detail = await _service.GetCourierAsync(pathao.Id);
        var sandbox = detail.Environments.Single(e => e.Environment == CourierEnvironment.Sandbox);
        sandbox.KeysSaved.ShouldBeTrue();
        detail.Environments.Single(e => e.Environment == CourierEnvironment.Live).KeysSaved.ShouldBeFalse();
        sandbox.Fields.First(f => f.Key == "client_secret").Display.ShouldEndWith(" abcd");
        sandbox.Fields.First(f => f.Key == "client_secret").Display.ShouldNotContain("SECRET");
        sandbox.Fields.First(f => f.Key == "username").Display.ShouldBe("ops@example.test");
        sandbox.Fields.First(f => f.Key == "base_url").Display.ShouldBe("https://sandbox.example.test");   // trailing slash removed

        (await _service.GetCourierCredentialRevealAsync(pathao.Id, new CourierCredentialKeyDto { Environment = CourierEnvironment.Sandbox, Key = "client_secret" }))
            .ShouldBe("SECRET-abcd");
        (await _service.GetCourierAsync(pathao.Id)).Logs.ShouldContain(l => l.Action == "Key revealed: Client secret");
        (await _service.GetCourierAsync(pathao.Id)).Logs.ShouldContain(l => l.Action == "Key changed: Password");

        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateCourierCredentialAsync(pathao.Id,
            new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = "base_url", Value = "http://insecure.test" }));
        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateCourierCredentialAsync(pathao.Id,
            new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = "not_a_key", Value = "x" }));
    }

    [Fact]
    public async Task Should_Connect_Reuse_The_Token_And_Forget_It_When_The_Login_Changes()
    {
        FakePathaoServer.Reset(PathaoHappy);
        var pathao = await PathaoAsync();
        await SaveSandboxKeysAsync(pathao.Id);

        var test = await _service.TestCourierAsync(pathao.Id);
        test.Ok.ShouldBeTrue();
        var tokenCall = FakePathaoServer.Requests.Single(r => r.Path == "/aladdin/api/v1/issue-token");
        using (var doc = JsonDocument.Parse(tokenCall.Body))
        {
            doc.RootElement.GetProperty("grant_type").GetString().ShouldBe("password");
            doc.RootElement.GetProperty("client_id").GetString().ShouldBe("CLIENT-ID-1234");
            doc.RootElement.GetProperty("username").GetString().ShouldBe("ops@example.test");
        }
        (await _service.GetOverviewAsync()).Couriers.First(c => c.Id == pathao.Id).Status.ShouldBe("connected");

        // A second call reuses the saved token instead of asking for a new one.
        var cities = await _service.GetPathaoCitiesAsync(pathao.Id);
        cities.Select(c => c.Name).ShouldBe(new[] { "Dhaka", "Chittagong" });
        FakePathaoServer.Requests.Count(r => r.Path == "/aladdin/api/v1/issue-token").ShouldBe(1);
        FakePathaoServer.Requests.Single(r => r.Path == "/aladdin/api/v1/city-list").Auth.ShouldBe("ACCESS-1");

        // Changing the password throws the token away.
        await _service.UpdateCourierCredentialAsync(pathao.Id, new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = "password", Value = "new-pass" });
        (await _service.GetOverviewAsync()).Couriers.First(c => c.Id == pathao.Id).Status.ShouldBe("keys");
    }

    [Fact]
    public async Task Should_Report_Pathao_Errors_Without_Throwing_From_Test()
    {
        FakePathaoServer.Reset((_, _) => (HttpStatusCode.Unauthorized, """{"message":"Invalid credentials","type":"error","code":401,"errors":{"client_secret":["The client secret is wrong."]}}"""));
        var pathao = await PathaoAsync();
        await SaveSandboxKeysAsync(pathao.Id);

        var test = await _service.TestCourierAsync(pathao.Id);
        test.Ok.ShouldBeFalse();
        test.Message.ShouldContain("Invalid credentials");
        test.Message.ShouldContain("client_secret: The client secret is wrong.");

        // The failure is in the call log even though the request failed.
        var detail = await _service.GetCourierAsync(pathao.Id);
        detail.Logs.ShouldContain(l => l.Action == "Token" && l.IsError && l.StatusCode == 401);
        (await _service.GetOverviewAsync()).FailedCallsToday.ShouldBeGreaterThanOrEqualTo(1);
    }

    // ── sending parcels ───────────────────────────────────────────────────

    [Fact]
    public async Task Should_Send_A_Valid_Order_To_Pathao_And_Refuse_A_Bad_One()
    {
        FakePathaoServer.Reset(PathaoHappy);
        var pathao = await PathaoAsync();
        await SaveSandboxKeysAsync(pathao.Id);
        await _service.UpdateCourierSettingsAsync(pathao.Id, new UpdateCourierSettingsDto
        {
            DisplayName = pathao.DisplayName, ShortCode = pathao.ShortCode, Color = pathao.Color, IsEnabled = true,
            PickupStoreId = "5501", PickupStoreName = "Agrabad warehouse", DefaultDeliveryType = 48, DefaultItemType = 2, DefaultWeightKg = 1,
        });

        var good = await AddOrderAsync("ORD-T-1", "+880 1712-345678", "House 12, Road 4, Agrabad, Chattogram", 25750.4, "500 g", 3);
        var bad = await AddOrderAsync("ORD-T-2", "12345", "Short", 100);

        var page = await _service.GetShipmentsAsync(new GetShipmentsInput());
        var readyGood = page.Ready.Single(r => r.OrderId == good);
        readyGood.WeightKg.ShouldBe(1.5m);
        readyGood.Problems.ShouldBeEmpty();
        readyGood.SuggestedCourierId.ShouldBe(pathao.Id);
        page.Ready.Single(r => r.OrderId == bad).Problems.Count.ShouldBe(2);

        var results = await _service.SendParcelsAsync(new SendParcelsDto { OrderIds = new() { good, bad }, CourierAccountId = pathao.Id });
        results.Single(r => r.OrderId == good).Ok.ShouldBeTrue();
        results.Single(r => r.OrderId == good).ConsignmentId.ShouldBe("DF2609XYZ");
        results.Single(r => r.OrderId == bad).Ok.ShouldBeFalse();

        var sent = FakePathaoServer.Requests.Single(r => r.Path == "/aladdin/api/v1/orders");
        using (var doc = JsonDocument.Parse(sent.Body))
        {
            var b = doc.RootElement;
            b.GetProperty("store_id").GetInt32().ShouldBe(5501);
            b.GetProperty("recipient_phone").GetString().ShouldBe("01712345678");
            b.GetProperty("amount_to_collect").GetInt32().ShouldBe(25750);
            b.GetProperty("item_weight").GetString().ShouldBe("1.5");
            b.GetProperty("delivery_type").GetInt32().ShouldBe(48);
            b.TryGetProperty("special_instruction", out _).ShouldBeFalse();   // optional fields are left out, not null
        }

        page = await _service.GetShipmentsAsync(new GetShipmentsInput());
        page.Ready.ShouldNotContain(r => r.OrderId == good);
        var shipment = page.Shipments.Single(s => s.OrderId == good);
        shipment.Stage.ShouldBe("ready");
        shipment.DeliveryFee.ShouldBe(80m);
        (await _service.GetShipmentsAsync(new GetShipmentsInput { Filter = "DF2609" })).Shipments.ShouldHaveSingleItem();

        var refreshed = await _service.RefreshShipmentAsync(shipment.Id);
        refreshed.Status.ShouldBe("Delivered");
        refreshed.Stage.ShouldBe("delivered");
    }

    [Fact]
    public async Task Should_Hand_Parcels_To_The_Own_Team_Without_Any_Api()
    {
        var own = (await _service.GetOverviewAsync()).Couriers.First(c => c.Provider == CourierProvider.OwnDelivery);
        var order = await AddOrderAsync("ORD-T-3", "01812345678", "Plot 4, Nasirabad, Chattogram", 0, "27 kg");

        var ready = (await _service.GetShipmentsAsync(new GetShipmentsInput())).Ready.Single(r => r.OrderId == order);
        ready.Problems.ShouldContain(p => p.Contains("10 kg"));
        ready.SuggestedCourierId.ShouldBe(own.Id);

        var result = (await _service.SendParcelsAsync(new SendParcelsDto { OrderIds = new() { order }, CourierAccountId = own.Id })).Single();
        result.Ok.ShouldBeTrue();
        result.ConsignmentId.ShouldStartWith("OWN-");

        var steadfast = (await _service.GetOverviewAsync()).Couriers.First(c => c.Provider == CourierProvider.Steadfast);
        await Should.ThrowAsync<UserFriendlyException>(() => _service.SendParcelsAsync(new SendParcelsDto { OrderIds = new() { order }, CourierAccountId = steadfast.Id }));
    }

    [Fact]
    public async Task Should_Require_A_Pickup_Store_Before_Sending_To_Pathao()
    {
        FakePathaoServer.Reset(PathaoHappy);
        var pathao = await PathaoAsync();
        await SaveSandboxKeysAsync(pathao.Id);
        var order = await AddOrderAsync("ORD-T-4", "01712345678", "House 12, Road 4, Agrabad, Chattogram", 100);
        await Should.ThrowAsync<UserFriendlyException>(() => _service.SendParcelsAsync(new SendParcelsDto { OrderIds = new() { order }, CourierAccountId = pathao.Id }));
    }

    // ── webhook ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Accept_Status_Updates_Only_With_The_Webhook_Secret()
    {
        FakePathaoServer.Reset(PathaoHappy);
        var pathao = await PathaoAsync();
        await SaveSandboxKeysAsync(pathao.Id);
        await _service.UpdateCourierSettingsAsync(pathao.Id, new UpdateCourierSettingsDto
        {
            DisplayName = pathao.DisplayName, ShortCode = pathao.ShortCode, Color = pathao.Color, IsEnabled = true,
            PickupStoreId = "5501", DefaultDeliveryType = 48, DefaultItemType = 2, DefaultWeightKg = 1,
        });
        var order = await AddOrderAsync("ORD-T-5", "01712345678", "House 12, Road 4, Agrabad, Chattogram", 100);
        await _service.SendParcelsAsync(new SendParcelsDto { OrderIds = new() { order }, CourierAccountId = pathao.Id });

        var webhook = GetRequiredService<IShippingWebhookService>();
        const string body = """{"consignment_id":"DF2609XYZ","order_status":"In_Transit","event":"order.in-transit"}""";

        // No secret saved yet: refused.
        (await webhook.HandleAsync(pathao.Id, new Dictionary<string, string> { ["X-PATHAO-Signature"] = "anything" }, body)).StatusCode.ShouldBe(401);

        await _service.UpdateCourierCredentialAsync(pathao.Id, new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = CourierCatalog.WebhookSecret, Value = "hook-secret" });
        await _service.UpdateCourierCredentialAsync(pathao.Id, new UpdateCourierCredentialDto { Environment = CourierEnvironment.Sandbox, Key = CourierCatalog.WebhookReply, Value = "X-Reply-Check: ok-123" });

        (await webhook.HandleAsync(pathao.Id, new Dictionary<string, string> { ["X-PATHAO-Signature"] = "wrong" }, body)).StatusCode.ShouldBe(401);
        var shipments = await _service.GetShipmentsAsync(new GetShipmentsInput());
        shipments.Shipments.Single(s => s.OrderId == order).Status.ShouldBe("Pending");

        var (status, headers) = await webhook.HandleAsync(pathao.Id, new Dictionary<string, string> { ["x-pathao-signature"] = "hook-secret" }, body);
        status.ShouldBe(202);
        headers["X-Reply-Check"].ShouldBe("ok-123");
        var s = (await _service.GetShipmentsAsync(new GetShipmentsInput())).Shipments.Single(x => x.OrderId == order);
        s.Status.ShouldBe("In_Transit");
        s.Stage.ShouldBe("transit");
        (await _service.GetCourierAsync(pathao.Id)).LastWebhookNote.ShouldBe("In_Transit, consignment DF2609XYZ");

        // Pathao event without order_status: the status comes from the event name; the reason lands in the history.
        (await webhook.HandleAsync(pathao.Id, new Dictionary<string, string> { ["X-PATHAO-Signature"] = "hook-secret" },
            """{"consignment_id":"DF2609XYZ","event":"order.delivery-failed","reason":"Customer not reachable","updated_at":"2099-01-01 10:00:00"}""")).StatusCode.ShouldBe(202);
        var detail = await _service.GetShipmentDetailAsync(s.Id);
        detail.Shipment.Status.ShouldBe("Delivery Failed");
        detail.Shipment.Stage.ShouldBe("failed");
        detail.Events.First().Source.ShouldBe("sent");
        detail.Events.Last().Note.ShouldBe("Customer not reachable");
        detail.Events.Count(e => e.Source == "webhook").ShouldBe(2);
        detail.Order!.Number.ShouldBe("ORD-T-5");
        detail.Order.Items.Single().Quantity.ShouldBe(1);
        detail.CanTrack.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Answer_Pathaos_Webhook_Check_With_Its_Header()
    {
        var pathao = await PathaoAsync();
        var webhook = GetRequiredService<IShippingWebhookService>();

        // Works before any secret is saved, and changes nothing.
        var (status, headers) = await webhook.HandleAsync(pathao.Id, new Dictionary<string, string>(), """{"event":"webhook_integration"}""");
        status.ShouldBe(202);
        headers["X-Pathao-Merchant-Webhook-Integration-Secret"].ShouldBe("f3992ecc-59da-4cbe-a049-a13da2018d51");
        (await _service.GetCourierAsync(pathao.Id)).LastWebhookNote!.ShouldStartWith("Webhook connected");

        // Any other event still needs the secret.
        (await webhook.HandleAsync(pathao.Id, new Dictionary<string, string>(), """{"event":"order.delivered","consignment_id":"X"}""")).StatusCode.ShouldBe(401);
    }

    [Theory]
    [InlineData("order.pickup-requested", "Pickup Requested")]
    [InlineData("order.at-the-sorting-hub", "At The Sorting Hub")]
    [InlineData("order.delivered", "Delivered")]
    [InlineData(null, null)]
    public void Should_Turn_Pathao_Event_Names_Into_Statuses(string? eventName, string? expected) =>
        ShippingWebhookService.StatusFromEvent(eventName).ShouldBe(expected);

    // ── helpers ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("+880 1712-345678", "01712345678")]
    [InlineData("8801712345678", "01712345678")]
    [InlineData("01712345678", "01712345678")]
    [InlineData("1712345678", null)]
    [InlineData("02912345678", null)]
    public void Should_Normalise_Bangladeshi_Mobile_Numbers(string raw, string? expected) =>
        CourierCatalog.NormalizePhone(raw).ShouldBe(expected);

    [Theory]
    [InlineData("27 kg", 27)]
    [InlineData("500 g", 0.5)]
    [InlineData("1.2", 1.2)]
    [InlineData("2,5 kg", 2.5)]
    public void Should_Read_Product_Weights(string text, double kg) =>
        CourierCatalog.ParseWeightKg(text).ShouldBe((decimal)kg);
}
