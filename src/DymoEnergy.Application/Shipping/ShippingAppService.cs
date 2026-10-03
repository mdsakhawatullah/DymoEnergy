using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Orders;
using DymoEnergy.Permissions;
using DymoEnergy.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Shipping;

[Authorize(DymoEnergyPermissions.Shipping.Default)]
public class ShippingAppService : ApplicationService, IShippingAppService
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static readonly OrderStatus[] ShippableStatuses = { OrderStatus.Confirmed, OrderStatus.Processing };

    private readonly IRepository<CourierAccount, int> _accounts;
    private readonly IRepository<CourierApiLog, int>  _logs;
    private readonly IRepository<Shipment, int>       _shipments;
    private readonly IRepository<Order, int>          _orders;
    private readonly IRepository<OrderItem, int>      _orderItems;
    private readonly IRepository<Product, int>        _products;
    private readonly CourierVault     _vault;
    private readonly CourierLogWriter _log;
    private readonly PathaoClient     _pathao;
    private readonly OrderFactsBuilder _facts;
    private readonly IRepository<CourierRule, int> _rules;

    public ShippingAppService(
        IRepository<CourierAccount, int> accounts, IRepository<CourierApiLog, int> logs, IRepository<Shipment, int> shipments,
        IRepository<Order, int> orders, IRepository<OrderItem, int> orderItems, IRepository<Product, int> products,
        CourierVault vault, CourierLogWriter log, PathaoClient pathao, OrderFactsBuilder facts, IRepository<CourierRule, int> rules)
    {
        _accounts = accounts; _logs = logs; _shipments = shipments; _orders = orders; _orderItems = orderItems;
        _products = products; _vault = vault; _log = log; _pathao = pathao; _facts = facts; _rules = rules;
    }

    // ══ OVERVIEW ═════════════════════════════════════════════════════════════

    public async Task<ShippingOverviewDto> GetOverviewAsync()
    {
        await EnsureSeedAsync();
        var accounts = (await _accounts.GetListAsync()).OrderBy(a => a.Order).ThenBy(a => a.Id).ToList();
        var summaries = new List<CourierSummaryDto>();
        foreach (var a in accounts) summaries.Add(await SummaryAsync(a));

        var shipments = await _shipments.GetListAsync();
        var today = Clock.Now.Date;
        var failed = await _logs.GetListAsync(l => l.IsError && l.Time >= today);
        var connected = summaries.Where(s => s.Status is "connected" or "always").ToList();

        return new ShippingOverviewDto
        {
            Couriers = summaries,
            TotalCount = summaries.Count,
            ConnectedCount = connected.Count,
            ConnectedNames = string.Join(", ", connected.Select(s => s.DisplayName)),
            AnyLiveKeys = summaries.Any(s => s.Status == "connected" && s.ActiveEnvironment == CourierEnvironment.Live),
            ParcelsMoving = shipments.Count(s => CourierCatalog.Stage(s.Status) is "ready" or "picked" or "transit"),
            ReadyToSend = (await ReadyOrdersAsync(accounts)).Count,
            CodDelivered = shipments.Where(s => CourierCatalog.Stage(s.Status) == "delivered").Sum(s => s.CodAmount),
            FailedCallsToday = failed.Count,
            LastFailedCall = failed.OrderByDescending(l => l.Time).FirstOrDefault()?.Result,
        };
    }

    // ══ COURIERS & KEYS ══════════════════════════════════════════════════════

    public async Task<CourierDetailDto> GetCourierAsync(int id)
    {
        await EnsureSeedAsync();
        return await DetailAsync(await _accounts.GetAsync(id));
    }

    [Authorize(DymoEnergyPermissions.Shipping.ManageKeys)]
    public async Task<CourierDetailDto> CreateCourierAsync(CreateCourierDto input)
    {
        await EnsureSeedAsync();
        var account = new CourierAccount
        {
            Provider = input.Provider, DisplayName = input.DisplayName.Trim(), ShortCode = input.ShortCode.Trim().ToUpperInvariant(),
            Color = input.Color, Order = (await _accounts.GetListAsync()).Select(a => a.Order).DefaultIfEmpty(0).Max() + 1,
        };
        await _accounts.InsertAsync(account, autoSave: true);
        return await DetailAsync(account);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<CourierDetailDto> UpdateCourierSettingsAsync(int id, UpdateCourierSettingsDto input)
    {
        if (input.DefaultDeliveryType is not (48 or 12)) throw new UserFriendlyException("Delivery type must be 48 (normal) or 12 (on demand).");
        if (input.DefaultItemType is not (1 or 2)) throw new UserFriendlyException("Item type must be 1 (document) or 2 (parcel).");

        var a = await _accounts.GetAsync(id);
        a.DisplayName = input.DisplayName.Trim(); a.ShortCode = input.ShortCode.Trim().ToUpperInvariant(); a.Color = input.Color;
        a.IsEnabled = input.IsEnabled; a.PickupStoreId = Clean(input.PickupStoreId); a.PickupStoreName = Clean(input.PickupStoreName);
        a.DefaultDeliveryType = input.DefaultDeliveryType; a.DefaultItemType = input.DefaultItemType; a.DefaultWeightKg = input.DefaultWeightKg;
        a.CodFeePercent = input.CodFeePercent; a.PayoutSchedule = Clean(input.PayoutSchedule);
        await _accounts.UpdateAsync(a, autoSave: true);
        return await DetailAsync(a);
    }

    [Authorize(DymoEnergyPermissions.Shipping.ManageKeys)]
    public async Task<CourierDetailDto> UpdateCourierCredentialAsync(int id, UpdateCourierCredentialDto input)
    {
        var a = await _accounts.GetAsync(id);
        var field = CourierCatalog.FindField(a.Provider, input.Key)
            ?? throw new UserFriendlyException($"“{input.Key}” is not a key of {a.DisplayName}.");
        var value = input.Value?.Trim();

        if (input.Key == "base_url" && !string.IsNullOrEmpty(value))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new UserFriendlyException("The base URL must be a full https:// address.");
            value = value.TrimEnd('/');
        }
        if (input.Key == CourierCatalog.WebhookReply && !string.IsNullOrEmpty(value) && !value.Contains(':'))
            throw new UserFriendlyException("Write the reply header as Name: Value.");

        await _vault.SetAsync(id, input.Environment, input.Key, value);

        // A changed login makes the old token meaningless.
        if (input.Key is "client_id" or "client_secret" or "username" or "password" or "base_url")
            await ForgetTokenAsync(id, input.Environment);

        await _log.WriteAsync(id, input.Environment, string.IsNullOrEmpty(value) ? $"Key cleared: {field.Label}" : $"Key changed: {field.Label}");
        return await DetailAsync(a);
    }

    /// <summary>Shows one secret in full. Every reveal is written to the call log with the user.</summary>
    [Authorize(DymoEnergyPermissions.Shipping.ManageKeys)]
    public async Task<string> GetCourierCredentialRevealAsync(int id, CourierCredentialKeyDto input)
    {
        var a = await _accounts.GetAsync(id);
        var field = CourierCatalog.FindField(a.Provider, input.Key) ?? throw new UserFriendlyException("Unknown key.");
        var value = await _vault.GetAsync(id, input.Environment, input.Key) ?? "";
        await _log.WriteAsync(id, input.Environment, $"Key revealed: {field.Label}");
        return value;
    }

    [Authorize(DymoEnergyPermissions.Shipping.ManageKeys)]
    public async Task<CourierDetailDto> UpdateCourierEnvironmentAsync(int id, SetCourierEnvironmentDto input)
    {
        var a = await _accounts.GetAsync(id);
        if (a.ActiveEnvironment != input.Environment)
        {
            a.ActiveEnvironment = input.Environment;
            await _accounts.UpdateAsync(a, autoSave: true);
            await _log.WriteAsync(id, input.Environment, $"Switched to {input.Environment} keys");
        }
        return await DetailAsync(a);
    }

    public async Task<CourierTestResultDto> TestCourierAsync(int id)
    {
        var a = await _accounts.GetAsync(id);
        return await TestAsync(a, forceNew: false);
    }

    [Authorize(DymoEnergyPermissions.Shipping.ManageKeys)]
    public async Task<CourierTestResultDto> RefreshCourierTokenAsync(int id)
    {
        var a = await _accounts.GetAsync(id);
        return await TestAsync(a, forceNew: true);
    }

    public async Task<List<CourierTestResultDto>> TestAllCouriersAsync()
    {
        await EnsureSeedAsync();
        var results = new List<CourierTestResultDto>();
        foreach (var a in (await _accounts.GetListAsync()).Where(a => a.IsEnabled).OrderBy(a => a.Order))
        {
            var r = await TestAsync(a, forceNew: false);
            r.Message = $"{a.DisplayName}: {r.Message}";
            results.Add(r);
        }
        return results;
    }

    /// <summary>Removes every saved key and token of this courier (both environments).</summary>
    [Authorize(DymoEnergyPermissions.Shipping.ManageKeys)]
    public async Task<CourierDetailDto> DisconnectCourierAsync(int id)
    {
        var a = await _accounts.GetAsync(id);
        await _vault.ClearAsync(id);
        a.PickupStoreId = null; a.PickupStoreName = null;
        await _accounts.UpdateAsync(a, autoSave: true);
        await _log.WriteAsync(id, a.ActiveEnvironment, "Disconnected: all keys removed");
        return await DetailAsync(a);
    }

    private async Task<CourierTestResultDto> TestAsync(CourierAccount a, bool forceNew)
    {
        if (CourierCatalog.IsManual(a.Provider))
            return new CourierTestResultDto { Ok = a.IsEnabled, Message = a.IsEnabled ? "No connection needed." : "Switched off." };
        if (!CourierCatalog.HasApi(a.Provider))
            return new CourierTestResultDto { Ok = false, Message = "Keys can be saved, but this courier's API is not built into the app yet." };

        try
        {
            if (forceNew) await ForgetTokenAsync(a.Id, a.ActiveEnvironment);
            var conn = await ConnectAsync(a);
            var expires = await ExpiresAtAsync(a.Id, a.ActiveEnvironment);
            return new CourierTestResultDto { Ok = true, Message = $"Connected to {a.ActiveEnvironment}. Token valid until {expires:d MMM, HH:mm}.", TokenExpiresAt = expires };
        }
        catch (UserFriendlyException ex)
        {
            return new CourierTestResultDto { Ok = false, Message = ex.Message };
        }
    }

    // ══ PATHAO LOOKUPS ═══════════════════════════════════════════════════════

    public async Task<List<PathaoStoreDto>> GetPathaoStoresAsync(int id)
    {
        var (a, conn) = await PathaoAsync(id);
        var call = await _pathao.GetStoresAsync(conn.BaseUrl, conn.Token);
        await _log.WriteAsync(id, a.ActiveEnvironment, "Store list", call, $"{call.Data?.Count ?? 0} stores");
        return Unwrap(call);
    }

    [Authorize(DymoEnergyPermissions.Shipping.Edit)]
    public async Task<string> CreatePathaoStoreAsync(int id, CreatePathaoStoreDto input)
    {
        var (a, conn) = await PathaoAsync(id);
        var call = await _pathao.CreateStoreAsync(conn.BaseUrl, conn.Token, input);
        await _log.WriteAsync(id, a.ActiveEnvironment, "New store", call, input.Name);
        return Unwrap(call);
    }

    public async Task<List<PathaoLocationDto>> GetPathaoCitiesAsync(int id)
    {
        var (a, conn) = await PathaoAsync(id);
        var call = await _pathao.GetCitiesAsync(conn.BaseUrl, conn.Token);
        await _log.WriteAsync(id, a.ActiveEnvironment, "City list", call, $"{call.Data?.Count ?? 0} cities");
        return Unwrap(call);
    }

    public async Task<List<PathaoLocationDto>> GetPathaoZonesAsync(int id, int cityId)
    {
        var (a, conn) = await PathaoAsync(id);
        var call = await _pathao.GetZonesAsync(conn.BaseUrl, conn.Token, cityId);
        await _log.WriteAsync(id, a.ActiveEnvironment, "Zone list", call, $"{call.Data?.Count ?? 0} zones");
        return Unwrap(call);
    }

    public async Task<List<PathaoLocationDto>> GetPathaoAreasAsync(int id, int zoneId)
    {
        var (a, conn) = await PathaoAsync(id);
        var call = await _pathao.GetAreasAsync(conn.BaseUrl, conn.Token, zoneId);
        await _log.WriteAsync(id, a.ActiveEnvironment, "Area list", call, $"{call.Data?.Count ?? 0} areas");
        return Unwrap(call);
    }

    public async Task<PathaoPriceDto> GetPathaoPriceAsync(int id, PathaoPriceInputDto input)
    {
        var (a, conn) = await PathaoAsync(id);
        if (string.IsNullOrEmpty(a.PickupStoreId)) throw new UserFriendlyException("Choose a pickup store first.");
        var call = await _pathao.GetPriceAsync(conn.BaseUrl, conn.Token, a.PickupStoreId, input);
        await _log.WriteAsync(id, a.ActiveEnvironment, "Price check", call, call.Data == null ? null : $"৳{call.Data.FinalPrice:0.##}");
        return Unwrap(call);
    }

    // ══ SHIPMENTS ════════════════════════════════════════════════════════════

    public async Task<ShipmentsPageDto> GetShipmentsAsync(GetShipmentsInput input)
    {
        await EnsureSeedAsync();
        var accounts = await _accounts.GetListAsync();
        var ready = await ReadyOrdersAsync(accounts);
        var shipments = (await _shipments.GetListAsync()).OrderByDescending(s => s.CreationTime).ToList();
        var dtos = await MapShipmentsAsync(shipments, accounts);
        var today = Clock.Now.Date;

        var q = (input.Filter ?? "").Trim();
        var filtered = q.Length == 0 ? dtos : dtos.Where(s =>
            (s.ConsignmentId ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.OrderNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.RecipientName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            shipments.First(x => x.Id == s.Id).RecipientPhone.Contains(q)).ToList();

        return new ShipmentsPageDto
        {
            Ready = ready,
            Shipments = filtered.Take(200).ToList(),
            Counts = new ShipmentCountsDto
            {
                Ready = ready.Count,
                PickedUp = dtos.Count(s => s.Stage == "picked"),
                InTransit = dtos.Count(s => s.Stage is "transit" or "ready"),
                DeliveredToday = dtos.Count(s => s.Stage == "delivered" && (s.StatusAt ?? s.CreationTime).Date == today),
                FailedOrReturning = dtos.Count(s => s.Stage is "failed" or "returned"),
            },
        };
    }

    /// <summary>
    /// Hands the chosen orders to a courier. Each order is sent on its own, so one bad address
    /// does not stop the rest; the result says what happened to each.
    /// </summary>
    [Authorize(DymoEnergyPermissions.Shipping.Send)]
    public async Task<List<SendParcelResultDto>> SendParcelsAsync(SendParcelsDto input)
    {
        var a = await _accounts.GetAsync(input.CourierAccountId);
        if (!a.IsEnabled) throw new UserFriendlyException($"{a.DisplayName} is switched off.");
        if (!CourierCatalog.HasApi(a.Provider) && !CourierCatalog.IsManual(a.Provider))
            throw new UserFriendlyException($"{a.DisplayName}'s API is not built into the app yet, so parcels cannot be sent to it from here.");

        (string BaseUrl, string Token)? conn = null;
        if (a.Provider == CourierProvider.Pathao)
        {
            if (string.IsNullOrEmpty(a.PickupStoreId)) throw new UserFriendlyException("Choose a Pathao pickup store first (Couriers & keys → Pickup store).");
            conn = await ConnectAsync(a);
        }

        var accounts = await _accounts.GetListAsync();
        var ready = (await ReadyOrdersAsync(accounts)).ToDictionary(r => r.OrderId);
        var results = new List<SendParcelResultDto>();

        foreach (var orderId in input.OrderIds.Distinct())
        {
            if (!ready.TryGetValue(orderId, out var r))
            {
                results.Add(new SendParcelResultDto { OrderId = orderId, Ok = false, Message = "Not ready to send (already sent, cancelled or not confirmed)." });
                continue;
            }
            var order = await _orders.GetAsync(orderId);
            var phone = CourierCatalog.NormalizePhone(r.Phone);
            var weight = Math.Clamp(r.WeightKg, 0.5m, 10m);

            if (a.Provider == CourierProvider.Pathao && r.Problems.Count > 0)
            {
                results.Add(new SendParcelResultDto { OrderId = orderId, OrderNumber = r.OrderNumber, Ok = false, Message = string.Join(" ", r.Problems) });
                continue;
            }

            var shipment = new Shipment
            {
                OrderId = orderId, CourierAccountId = a.Id, Environment = a.ActiveEnvironment, MerchantOrderId = r.OrderNumber,
                RecipientName = r.CustomerName, RecipientPhone = phone ?? r.Phone ?? "", RecipientAddress = r.Address ?? "",
                CodAmount = r.Collect, WeightKg = r.WeightKg, DeliveryType = input.DeliveryType ?? a.DefaultDeliveryType,
                ItemType = input.ItemType ?? a.DefaultItemType, Note = Clean(input.SpecialInstruction), StatusAt = Clock.Now,
            };

            if (a.Provider == CourierProvider.Pathao)
            {
                var call = await _pathao.CreateOrderAsync(conn!.Value.BaseUrl, conn.Value.Token, new PathaoOrderRequest
                {
                    StoreId = a.PickupStoreId!, MerchantOrderId = r.OrderNumber, RecipientName = r.CustomerName, RecipientPhone = phone!,
                    RecipientAddress = r.Address!, DeliveryType = shipment.DeliveryType, ItemType = shipment.ItemType,
                    SpecialInstruction = shipment.Note, ItemQuantity = 1, ItemWeightKg = weight,
                    AmountToCollect = (int)Math.Round(r.Collect, MidpointRounding.AwayFromZero), ItemDescription = Truncate(r.Items, 250),
                });
                await _log.WriteAsync(a.Id, a.ActiveEnvironment, "New parcel", call, call.Data == null ? null : $"{call.Data.ConsignmentId} created");
                if (!call.Ok || string.IsNullOrEmpty(call.Data?.ConsignmentId))
                {
                    results.Add(new SendParcelResultDto { OrderId = orderId, OrderNumber = r.OrderNumber, Ok = false, Message = call.Error ?? "Pathao did not return a consignment id." });
                    continue;
                }
                shipment.ConsignmentId = call.Data!.ConsignmentId;
                shipment.Status = call.Data.Status;
                shipment.DeliveryFee = call.Data.DeliveryFee;
                await _shipments.InsertAsync(shipment, autoSave: true);
            }
            else
            {
                shipment.Status = "Pending";
                await _shipments.InsertAsync(shipment, autoSave: true);
                shipment.ConsignmentId = $"{a.ShortCode}-{shipment.Id:D5}";
                await _shipments.UpdateAsync(shipment, autoSave: true);
            }

            results.Add(new SendParcelResultDto
            {
                OrderId = orderId, OrderNumber = r.OrderNumber, Ok = true, ConsignmentId = shipment.ConsignmentId,
                DeliveryFee = shipment.DeliveryFee, Message = $"Sent to {a.DisplayName} as {shipment.ConsignmentId}.",
            });
        }
        return results;
    }

    /// <summary>Asks the courier for the latest status of one parcel.</summary>
    public async Task<ShipmentDto> RefreshShipmentAsync(int id)
    {
        var s = await _shipments.GetAsync(id);
        var a = await _accounts.GetAsync(s.CourierAccountId);
        if (a.Provider == CourierProvider.Pathao && !string.IsNullOrEmpty(s.ConsignmentId))
        {
            if (s.Environment != a.ActiveEnvironment)
                throw new UserFriendlyException($"This parcel was sent with the {s.Environment} keys; switch back to them to track it.");
            var conn = await ConnectAsync(a);
            var call = await _pathao.GetOrderInfoAsync(conn.BaseUrl, conn.Token, s.ConsignmentId);
            await _log.WriteAsync(a.Id, a.ActiveEnvironment, "Order info", call, call.Data?.Status);
            var info = Unwrap(call);
            s.Status = info.Status;
            s.StatusSlug = info.StatusSlug;
            s.StatusAt = info.UpdatedAt ?? Clock.Now;
            await _shipments.UpdateAsync(s, autoSave: true);
        }
        return (await MapShipmentsAsync(new List<Shipment> { s }, await _accounts.GetListAsync())).Single();
    }

    // ══ HELPERS ══════════════════════════════════════════════════════════════

    private async Task<(CourierAccount Account, (string BaseUrl, string Token) Conn)> PathaoAsync(int id)
    {
        var a = await _accounts.GetAsync(id);
        if (a.Provider != CourierProvider.Pathao) throw new UserFriendlyException("This only works for Pathao.");
        return (a, await ConnectAsync(a));
    }

    /// <summary>Returns a working access token: the saved one, a refreshed one, or a newly issued one.</summary>
    private async Task<(string BaseUrl, string Token)> ConnectAsync(CourierAccount a)
    {
        var env = a.ActiveEnvironment;
        var baseUrl = await _vault.GetAsync(a.Id, env, "base_url");
        var clientId = await _vault.GetAsync(a.Id, env, "client_id");
        var clientSecret = await _vault.GetAsync(a.Id, env, "client_secret");
        var username = await _vault.GetAsync(a.Id, env, "username");
        var password = await _vault.GetAsync(a.Id, env, "password");

        var missing = new List<string>();
        if (string.IsNullOrEmpty(baseUrl)) missing.Add("base URL");
        if (string.IsNullOrEmpty(clientId)) missing.Add("client ID");
        if (string.IsNullOrEmpty(clientSecret)) missing.Add("client secret");
        if (string.IsNullOrEmpty(username)) missing.Add("username");
        if (string.IsNullOrEmpty(password)) missing.Add("password");
        if (missing.Count > 0) throw new UserFriendlyException($"{a.DisplayName} {env} keys are missing: {string.Join(", ", missing)}.");

        var token = await _vault.GetAsync(a.Id, env, CourierCatalog.AccessToken);
        var expires = await ExpiresAtAsync(a.Id, env);
        if (!string.IsNullOrEmpty(token) && expires > Clock.Now.AddMinutes(5)) return (baseUrl!, token);

        var refresh = await _vault.GetAsync(a.Id, env, CourierCatalog.RefreshToken);
        if (!string.IsNullOrEmpty(refresh))
        {
            var r = await _pathao.RefreshTokenAsync(baseUrl!, clientId!, clientSecret!, refresh);
            await _log.WriteAsync(a.Id, env, "Token refresh", r, "Token refreshed");
            if (r.Ok && r.Data != null) return (baseUrl!, await SaveTokenAsync(a.Id, env, r.Data));
        }

        var issued = await _pathao.IssueTokenAsync(baseUrl!, clientId!, clientSecret!, username!, password!);
        await _log.WriteAsync(a.Id, env, "Token", issued, "Token issued");
        if (!issued.Ok || issued.Data == null)
            throw new UserFriendlyException($"Pathao refused the {env} keys: {issued.Error}");
        return (baseUrl!, await SaveTokenAsync(a.Id, env, issued.Data));
    }

    private async Task<string> SaveTokenAsync(int id, CourierEnvironment env, PathaoToken t)
    {
        var now = Clock.Now;
        await _vault.SetAsync(id, env, CourierCatalog.AccessToken, t.AccessToken);
        if (!string.IsNullOrEmpty(t.RefreshToken)) await _vault.SetAsync(id, env, CourierCatalog.RefreshToken, t.RefreshToken);
        await _vault.SetAsync(id, env, CourierCatalog.TokenIssuedAt, now.ToString("O", CultureInfo.InvariantCulture));
        await _vault.SetAsync(id, env, CourierCatalog.TokenExpiresAt, now.AddSeconds(t.ExpiresInSeconds).ToString("O", CultureInfo.InvariantCulture));
        return t.AccessToken;
    }

    private async Task ForgetTokenAsync(int id, CourierEnvironment env)
    {
        foreach (var k in new[] { CourierCatalog.AccessToken, CourierCatalog.RefreshToken, CourierCatalog.TokenExpiresAt, CourierCatalog.TokenIssuedAt })
            await _vault.SetAsync(id, env, k, null);
    }

    private async Task<DateTime?> ExpiresAtAsync(int id, CourierEnvironment env) =>
        DateTime.TryParse(await _vault.GetAsync(id, env, CourierCatalog.TokenExpiresAt), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null;

    private static T Unwrap<T>(PathaoCall<T> call) =>
        call.Ok && call.Data != null ? call.Data : throw new UserFriendlyException(call.Error ?? "Pathao did not answer as expected.");

    private async Task<CourierSummaryDto> SummaryAsync(CourierAccount a)
    {
        var dto = new CourierSummaryDto();
        await FillSummaryAsync(dto, a, await _vault.GetAllAsync(a.Id));
        return dto;
    }

    private async Task FillSummaryAsync(CourierSummaryDto dto, CourierAccount a, List<(CourierEnvironment Env, string Key, string Value, DateTime ChangedAt)> keys)
    {
        dto.Id = a.Id; dto.Provider = a.Provider; dto.DisplayName = a.DisplayName; dto.ShortCode = a.ShortCode; dto.Color = a.Color;
        dto.IsEnabled = a.IsEnabled; dto.Order = a.Order; dto.ActiveEnvironment = a.ActiveEnvironment;
        dto.ApiAvailable = CourierCatalog.HasApi(a.Provider); dto.IsManual = CourierCatalog.IsManual(a.Provider);

        var env = a.ActiveEnvironment;
        var required = CourierCatalog.Required[a.Provider];
        var saved = required.Length > 0 && required.All(k => keys.Any(x => x.Env == env && x.Key == k && x.Value.Length > 0));
        var tokenOk = keys.Any(x => x.Env == env && x.Key == CourierCatalog.AccessToken)
                      && DateTime.TryParse(keys.FirstOrDefault(x => x.Env == env && x.Key == CourierCatalog.TokenExpiresAt).Value,
                             CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var exp) && exp > Clock.Now;

        (dto.Status, dto.StatusText) = (a.IsEnabled, dto.IsManual, dto.ApiAvailable, saved, tokenOk) switch
        {
            (false, _, _, _, _) => ("off", "Switched off"),
            (true, true, _, _, _) => ("always", "Always on"),
            (true, false, true, true, true) => ("connected", $"{env} · token valid"),
            (true, false, true, true, false) => ("keys", $"{env} keys saved · not tested"),
            (true, false, false, true, _) => ("keys", "Keys saved · API not built yet"),
            _ => ("not-connected", "Not connected"),
        };
        await Task.CompletedTask;
    }

    private async Task<CourierDetailDto> DetailAsync(CourierAccount a)
    {
        var keys = await _vault.GetAllAsync(a.Id);
        var dto = new CourierDetailDto();
        await FillSummaryAsync(dto, a, keys);

        foreach (var env in new[] { CourierEnvironment.Live, CourierEnvironment.Sandbox })
        {
            var required = CourierCatalog.Required[a.Provider];
            dto.Environments.Add(new CourierEnvironmentDto
            {
                Environment = env,
                KeysSaved = required.Length > 0 && required.All(k => keys.Any(x => x.Env == env && x.Key == k && x.Value.Length > 0)),
                Fields = CourierCatalog.Fields[a.Provider].Select(f =>
                {
                    var hit = keys.FirstOrDefault(x => x.Env == env && x.Key == f.Key);
                    var has = hit.Key != null && hit.Value.Length > 0;
                    return new CourierFieldDto
                    {
                        Key = f.Key, Label = f.Label, Help = f.Help, IsSecret = f.IsSecret, HasValue = has,
                        Display = !has ? null : f.IsSecret ? CourierCatalog.Mask(hit.Value) : hit.Value,
                        ChangedAt = has ? hit.ChangedAt : null,
                    };
                }).ToList(),
            });
        }

        var active = a.ActiveEnvironment;
        DateTime? Parse(string key) =>
            DateTime.TryParse(keys.FirstOrDefault(x => x.Env == active && x.Key == key).Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null;

        dto.PickupStoreId = a.PickupStoreId; dto.PickupStoreName = a.PickupStoreName;
        dto.DefaultDeliveryType = a.DefaultDeliveryType; dto.DefaultItemType = a.DefaultItemType; dto.DefaultWeightKg = a.DefaultWeightKg;
        dto.CodFeePercent = a.CodFeePercent; dto.PayoutSchedule = a.PayoutSchedule;
        dto.TokenIssuedAt = Parse(CourierCatalog.TokenIssuedAt); dto.TokenExpiresAt = Parse(CourierCatalog.TokenExpiresAt);
        dto.WebhookPath = CourierCatalog.HasApi(a.Provider) ? $"/api/shipping/webhook/{a.Id}" : null;
        dto.LastWebhookAt = a.LastWebhookAt; dto.LastWebhookNote = a.LastWebhookNote;
        dto.LastParcelAt = (await _shipments.GetListAsync(s => s.CourierAccountId == a.Id)).Select(s => (DateTime?)s.CreationTime).DefaultIfEmpty().Max();

        var since = Clock.Now.AddDays(-1);
        dto.Logs = (await _logs.GetListAsync(l => l.CourierAccountId == a.Id && l.Time >= since))
            .OrderByDescending(l => l.Time).Take(50)
            .Select(l => new CourierLogDto
            {
                Time = l.Time, Environment = l.Environment, Action = l.Action, Method = l.Method, Endpoint = l.Endpoint,
                StatusCode = l.StatusCode, DurationMs = l.DurationMs, Result = l.Result, IsError = l.IsError,
            }).ToList();
        return dto;
    }

    /// <summary>Confirmed or processing orders that still need a courier, with what Pathao would reject.</summary>
    private async Task<List<ReadyOrderDto>> ReadyOrdersAsync(List<CourierAccount> accounts)
    {
        var sentOrderIds = (await _shipments.GetListAsync()).Where(s => CourierCatalog.Stage(s.Status) != "cancelled").Select(s => s.OrderId).ToHashSet();
        var orders = (await _orders.GetListAsync(o => ShippableStatuses.Contains(o.Status)))
            .Where(o => !sentOrderIds.Contains(o.Id) && o.ShipmentType != OrderShipmentType.Pickup)
            .OrderBy(o => o.OrderDate).ThenBy(o => o.Id).Take(200).ToList();
        if (orders.Count == 0) return new List<ReadyOrderDto>();

        var pathao = accounts.Where(a => a.IsEnabled && a.Provider == CourierProvider.Pathao).OrderBy(a => a.Order).FirstOrDefault();
        var facts = await _facts.BuildAsync(orders, pathao?.DefaultWeightKg ?? 1m);
        var rules = (await _rules.GetListAsync()).OrderBy(r => r.Order).ThenBy(r => r.Id).ToList();
        var byId = accounts.ToDictionary(a => a.Id);

        return facts.Select(f =>
        {
            var problems = new List<string>();
            if (f.CustomerName.Length is < 3 or > 100) problems.Add("Recipient name must be 3–100 characters.");
            if (CourierCatalog.NormalizePhone(f.Phone) == null) problems.Add("Phone must be an 11-digit mobile number.");
            if (f.Address.Length is < 10 or > 220) problems.Add("Address must be 10–220 characters.");
            if (f.TotalWeightKg > 10) problems.Add($"{f.TotalWeightKg:0.#} kg is over Pathao’s 10 kg limit.");

            var rule = CourierRuleEngine.FirstMatch(rules, f);
            CourierAccount? suggested = null;
            if (rule?.CourierAccountId is int cid && byId.TryGetValue(cid, out var ca) && ca.IsEnabled) suggested = ca;
            else if (rules.Count == 0)
            {
                // No rules set up yet: Pathao for parcels it accepts, the own team for the rest.
                var own = accounts.Where(a => a.IsEnabled && a.Provider == CourierProvider.OwnDelivery).OrderBy(a => a.Order).FirstOrDefault();
                suggested = f.TotalWeightKg > 10 || f.NeedsInstallation ? own ?? pathao : pathao ?? own;
            }

            return new ReadyOrderDto
            {
                OrderId = f.OrderId, OrderNumber = f.OrderNumber, CustomerName = f.CustomerName, Phone = f.Phone, Address = f.Address,
                Items = f.ItemsText, WeightKg = f.TotalWeightKg, WeightGuessed = f.WeightGuessed, Collect = f.Cod,
                SuggestedCourierId = suggested?.Id, SuggestedCourierName = suggested?.DisplayName, Problems = problems,
                RuleNumber = rule == null ? null : rules.Where(r => r.IsEnabled).ToList().IndexOf(rule) + 1,
                NoParcel = rule?.NoParcel == true,
            };
        }).ToList();
    }

    private async Task<List<ShipmentDto>> MapShipmentsAsync(List<Shipment> shipments, List<CourierAccount> accounts)
    {
        var orderIds = shipments.Select(s => s.OrderId).Distinct().ToList();
        var numbers = (await _orders.GetListAsync(o => orderIds.Contains(o.Id))).ToDictionary(o => o.Id, o => o.OrderNumber ?? $"#{o.Id}");
        var byId = accounts.ToDictionary(a => a.Id);
        return shipments.Select(s =>
        {
            byId.TryGetValue(s.CourierAccountId, out var a);
            return new ShipmentDto
            {
                Id = s.Id, OrderId = s.OrderId, OrderNumber = numbers.GetValueOrDefault(s.OrderId, $"#{s.OrderId}"), ConsignmentId = s.ConsignmentId,
                CourierAccountId = s.CourierAccountId, CourierName = a?.DisplayName ?? "Courier", CourierColor = a?.Color ?? "#6B7280",
                Environment = s.Environment, Status = s.Status, Stage = CourierCatalog.Stage(s.Status), StatusAt = s.StatusAt,
                CreationTime = s.CreationTime, RecipientName = s.RecipientName, RecipientAddress = s.RecipientAddress,
                CodAmount = s.CodAmount, DeliveryFee = s.DeliveryFee, DeliveryType = s.DeliveryType,
            };
        }).ToList();
    }

    private async Task EnsureSeedAsync()
    {
        if (await _accounts.GetCountAsync() > 0) return;
        await SeedLock.WaitAsync();
        try
        {
            if (await _accounts.GetCountAsync() > 0) return;
            var order = 0;
            await _accounts.InsertManyAsync(CourierCatalog.Seed.Select(s => new CourierAccount
            {
                Provider = s.Provider, DisplayName = s.Name, ShortCode = s.Code, Color = s.Color, Order = ++order,
                // Couriers without an app integration start switched off so nobody sends to them by mistake.
                IsEnabled = CourierCatalog.HasApi(s.Provider) || CourierCatalog.IsManual(s.Provider),
            }), autoSave: true);
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
