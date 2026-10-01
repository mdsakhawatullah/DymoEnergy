using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Categories;
using DymoEnergy.Orders;
using DymoEnergy.Permissions;
using DymoEnergy.Products;
using DymoEnergy.QuoteRequests;
using DymoEnergy.SalesInvoices;
using DymoEnergy.Settings;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.SettingManagement;

namespace DymoEnergy.Analytics;

/// <summary>
/// Analytics &amp; Reporting. Loads one <see cref="AnalyticsSnapshot"/> per request and
/// aggregates in memory — the data volume of a solar shop (thousands of invoices a year)
/// makes this far simpler than hand-tuned SQL, and nothing here is stored.
/// Uses the Sales Invoices permission so existing admin roles can open it without re-seeding.
/// </summary>
[Authorize(DymoEnergyPermissions.SalesInvoices.Default)]
public partial class AnalyticsAppService : ApplicationService, IAnalyticsAppService
{
    /// <summary>History used for velocity, slow stock, warranties and "new customer" checks.</summary>
    private const int HistoryDays = 400;

    private static readonly TimeZoneInfo Dhaka = FindDhaka();

    private readonly IRepository<SalesInvoice, int>        _invoices;
    private readonly IRepository<SalesInvoiceItem, int>    _invoiceItems;
    private readonly IRepository<SalesInvoicePayment, int> _payments;
    private readonly IRepository<Order, int>               _orders;
    private readonly IRepository<OrderItem, int>           _orderItems;
    private readonly IRepository<Product, int>             _products;
    private readonly IRepository<Category, int>            _categories;
    private readonly IRepository<QuoteRequest, int>        _quotes;
    private readonly IIdentityUserRepository               _users;
    private readonly ISettingManager                       _settingManager;

    public AnalyticsAppService(
        IRepository<SalesInvoice, int>        invoices,
        IRepository<SalesInvoiceItem, int>    invoiceItems,
        IRepository<SalesInvoicePayment, int> payments,
        IRepository<Order, int>               orders,
        IRepository<OrderItem, int>           orderItems,
        IRepository<Product, int>             products,
        IRepository<Category, int>            categories,
        IRepository<QuoteRequest, int>        quotes,
        IIdentityUserRepository               users,
        ISettingManager                       settingManager)
    {
        _invoices       = invoices;
        _invoiceItems   = invoiceItems;
        _payments       = payments;
        _orders         = orders;
        _orderItems     = orderItems;
        _products       = products;
        _categories     = categories;
        _quotes         = quotes;
        _users          = users;
        _settingManager = settingManager;
    }

    // ── Filters & settings ───────────────────────────────────────────────────

    public async Task<List<string>> GetChannelsAsync()
    {
        var query    = await _invoices.GetQueryableAsync();
        var channels = await AsyncExecuter.ToListAsync(
            query.Where(i => i.Channel != null && i.Channel != "").Select(i => i.Channel!).Distinct());

        if (await AsyncExecuter.AnyAsync(await _orders.GetQueryableAsync()))
            channels.Add(SaleRecord.OnlineChannel);

        return channels.Distinct().OrderBy(c => c).ToList();
    }

    public async Task SetSalesTargetAsync(SetSalesTargetDto input)
    {
        var value = Math.Max(0, Math.Round(input.MonthlyTarget));
        await _settingManager.SetGlobalAsync(DymoEnergySettings.MonthlySalesTarget, value.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<double> GetMonthlyTargetAsync()
    {
        var raw = await SettingProvider.GetOrNullAsync(DymoEnergySettings.MonthlySalesTarget);
        return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    // ── Snapshot loading ─────────────────────────────────────────────────────

    private async Task<AnalyticsSnapshot> LoadAsync(AnalyticsFilterDto input)
    {
        var now   = DateTime.UtcNow;
        var to    = input.DateTo.HasValue   ? Utc(input.DateTo.Value)   : now;
        var from  = input.DateFrom.HasValue ? Utc(input.DateFrom.Value) : to.AddDays(-30);
        if (from > to) (from, to) = (to, from);
        var span     = to - from;
        var prevFrom = from - span;
        var historyFrom = new[] { prevFrom, now.AddDays(-HistoryDays) }.Min();
        var channel  = string.IsNullOrWhiteSpace(input.Channel) ? null : input.Channel.Trim();

        // ── Invoices: history window + anything still owing ─────────────────
        var invQuery = await _invoices.GetQueryableAsync();
        var invoices = await AsyncExecuter.ToListAsync(invQuery.Where(i =>
            i.InvoiceDate >= historyFrom ||
            (i.BalanceDue > 0 && (i.Status == SalesInvoiceStatus.Issued ||
                                  i.Status == SalesInvoiceStatus.PartiallyPaid ||
                                  i.Status == SalesInvoiceStatus.Overdue))));

        // Payments in range may belong to older invoices — pull those in too
        var payQuery = await _payments.GetQueryableAsync();
        var payments = await AsyncExecuter.ToListAsync(payQuery.Where(p => p.PaidOn >= historyFrom));
        var loadedIds  = invoices.Select(i => i.Id).ToHashSet();
        var missingIds = payments.Select(p => p.InvoiceId).Where(id => !loadedIds.Contains(id)).Distinct().ToList();
        if (missingIds.Count > 0)
            invoices.AddRange(await AsyncExecuter.ToListAsync(invQuery.Where(i => missingIds.Contains(i.Id))));

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var itemQuery  = await _invoiceItems.GetQueryableAsync();
        var invItems   = (await AsyncExecuter.ToListAsync(itemQuery.Where(i => invoiceIds.Contains(i.InvoiceId))))
            .GroupBy(i => i.InvoiceId)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id).ToList());

        // ── Web orders ───────────────────────────────────────────────────────
        var ordQuery = await _orders.GetQueryableAsync();
        var orders   = await AsyncExecuter.ToListAsync(ordQuery.Where(o =>
            o.OrderDate >= historyFrom ||
            (o.BalanceDue > 0 && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded && o.Status != OrderStatus.Returned)));
        var orderIds  = orders.Select(o => o.Id).ToList();
        var oiQuery   = await _orderItems.GetQueryableAsync();
        var ordItems  = (await AsyncExecuter.ToListAsync(oiQuery.Where(i => orderIds.Contains(i.OrderId))))
            .GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id).ToList());

        // ── Catalogue ────────────────────────────────────────────────────────
        var categories = (await _categories.GetListAsync()).ToDictionary(c => c.Id, c => c.Name ?? "Other");
        var products   = (await _products.GetListAsync()).Select(p => new ProductInfo
        {
            Id       = p.Id,
            Name     = p.Name ?? $"Product {p.Id}",
            Category = categories.TryGetValue(p.CategoryId, out var c) ? c : "Other",
            Stock    = p.StockQuantity,
            Price    = p.DiscountPrice is > 0 ? p.DiscountPrice.Value : p.Price,
            IsActive = p.IsActive,
            Sku      = p.Sku,
            Vendor   = string.IsNullOrWhiteSpace(p.VendorName) ? null : p.VendorName,
        }).ToList();

        // ── Build sale records ──────────────────────────────────────────────
        var sales = new List<SaleRecord>();

        foreach (var i in invoices)
        {
            var items = invItems.TryGetValue(i.Id, out var list) ? list : new();
            var live  = i.Status is not (SalesInvoiceStatus.Draft or SalesInvoiceStatus.Cancelled or SalesInvoiceStatus.Refunded);
            sales.Add(new SaleRecord
            {
                Source             = "invoice",
                Id                 = i.Id,
                Number             = i.InvoiceNumber,
                Date               = Utc(i.InvoiceDate),
                LocalDate          = ToDhaka(i.InvoiceDate).Date,
                LocalCreated       = ToDhaka(i.CreationTime == default ? i.InvoiceDate : i.CreationTime),
                Channel            = string.IsNullOrWhiteSpace(i.Channel) ? "Showroom" : i.Channel!,
                CustomerName       = i.CustomerName,
                Phone              = i.CustomerPhone,
                CustomerKey        = AnalyticsText.CustomerKey(i.CustomerPhone, i.CustomerName),
                Location           = AnalyticsText.Location(i.BillingAddress ?? i.ShippingAddress),
                Total              = i.GrandTotal,
                Paid               = i.AmountPaid,
                Balance            = i.BalanceDue,
                Tax                = i.TaxTotal,
                LineDiscount       = Math.Max(0, i.DiscountTotal - i.AdditionalDiscount),
                AdditionalDiscount = i.AdditionalDiscount,
                DiscountNote       = i.DiscountNote,
                DueDate            = i.DueDate.HasValue ? Utc(i.DueDate.Value) : null,
                CreatorId          = i.CreatorId,
                PaymentMethod      = i.PaymentMethod.HasValue ? MethodLabel(i.PaymentMethod.Value) : null,
                Counted            = live,
                IsOpen             = live && i.Status != SalesInvoiceStatus.Paid,
                Refunded           = i.Status == SalesInvoiceStatus.Refunded,
                RefundDate         = i.Status == SalesInvoiceStatus.Refunded ? Utc(i.LastModificationTime ?? i.InvoiceDate) : null,
                Items = items.Select(x => new SaleItem
                {
                    ProductId = x.ProductId,
                    Name      = string.IsNullOrWhiteSpace(x.ProductName) ? "Item" : x.ProductName!.Trim(),
                    Quantity  = x.Quantity,
                    Total     = x.LineTotal,
                    Warranty  = x.Warranty,
                    Serials   = x.SerialNumbers,
                    Sku       = x.Sku,
                    Tax       = x.TaxAmount,
                }).ToList(),
            });
        }

        foreach (var o in orders)
        {
            var items    = ordItems.TryGetValue(o.Id, out var list) ? list : new();
            var dead     = o.Status is OrderStatus.Cancelled or OrderStatus.Refunded or OrderStatus.Returned;
            var refunded = o.Status is OrderStatus.Refunded or OrderStatus.Returned;
            sales.Add(new SaleRecord
            {
                Source        = "order",
                Id            = o.Id,
                Number        = o.OrderNumber,
                Date          = Utc(o.OrderDate),
                LocalDate     = ToDhaka(o.OrderDate).Date,
                LocalCreated  = ToDhaka(o.CreationTime == default ? o.OrderDate : o.CreationTime),
                Channel       = SaleRecord.OnlineChannel,
                CustomerName  = o.CustomerName,
                Phone         = o.CustomerPhone,
                CustomerKey   = AnalyticsText.CustomerKey(o.CustomerPhone, o.CustomerName),
                Location      = AnalyticsText.Location(o.DeliveryAddress ?? o.BillingAddress),
                Total         = o.GrandTotal,
                Paid          = o.AmountPaid,
                Balance       = o.BalanceDue,
                Tax           = o.TaxTotal,
                LineDiscount  = o.DiscountTotal,
                Voucher       = o.VoucherCode,
                VoucherAmount = o.VoucherAmount,
                IsCod         = o.PaymentType == OrderPaymentType.CashOnDelivery,
                PaymentMethod = o.PaymentType.HasValue ? OrderMethodLabel(o.PaymentType) : null,
                Counted       = !dead,
                IsOpen        = !dead,
                Refunded      = refunded,
                RefundDate    = refunded ? Utc(o.LastModificationTime ?? o.OrderDate) : null,
                Items = items.Select(x => new SaleItem
                {
                    ProductId = x.ProductId,
                    Name      = string.IsNullOrWhiteSpace(x.ProductName) ? "Item" : x.ProductName!.Trim(),
                    Quantity  = x.Quantity,
                    Total     = x.LineTotal,
                    Sku       = x.Sku,
                    Tax       = x.TaxAmount,
                }).ToList(),
            });
        }

        if (channel != null)
            sales = sales.Where(s => string.Equals(s.Channel, channel, StringComparison.OrdinalIgnoreCase)).ToList();

        // ── Payments (money in) ─────────────────────────────────────────────
        var invById   = invoices.ToDictionary(i => i.Id);
        var paidRows  = payments.Select(p =>
        {
            var inv = invById.TryGetValue(p.InvoiceId, out var x) ? x : null;
            return new PaymentRecord
            {
                Date      = Utc(p.PaidOn),
                Amount    = p.Amount,
                Method    = MethodLabel(p.Method),
                Channel   = string.IsNullOrWhiteSpace(inv?.Channel) ? "Showroom" : inv!.Channel!,
                Invoice   = inv?.InvoiceNumber,
                Customer  = inv?.CustomerName,
                Reference = p.ReferenceNumber,
            };
        }).ToList();

        // Invoices paid before the payments ledger existed: count their AmountPaid once
        var ledgered = payments.Select(p => p.InvoiceId).ToHashSet();
        paidRows.AddRange(invoices
            .Where(i => i.AmountPaid > 0 && !ledgered.Contains(i.Id) && i.Status != SalesInvoiceStatus.Draft)
            .Select(i => new PaymentRecord
            {
                Date     = Utc(i.PaymentDate ?? i.InvoiceDate),
                Amount   = i.AmountPaid,
                Method   = i.PaymentMethod.HasValue ? MethodLabel(i.PaymentMethod.Value) : "Other",
                Channel  = string.IsNullOrWhiteSpace(i.Channel) ? "Showroom" : i.Channel!,
                Invoice  = i.InvoiceNumber,
                Customer = i.CustomerName,
            }));

        paidRows.AddRange(orders
            .Where(o => o.AmountPaid > 0 && o.Status != OrderStatus.Cancelled)
            .Select(o => new PaymentRecord
            {
                Date     = Utc(o.PaymentDate ?? o.OrderDate),
                Amount   = o.AmountPaid,
                Method   = OrderMethodLabel(o.PaymentType),
                Channel  = SaleRecord.OnlineChannel,
                Invoice  = o.OrderNumber,
                Customer = o.CustomerName,
            }));

        if (channel != null)
            paidRows = paidRows.Where(p => string.Equals(p.Channel, channel, StringComparison.OrdinalIgnoreCase)).ToList();

        // ── Quote requests ──────────────────────────────────────────────────
        var quoteQuery = await _quotes.GetQueryableAsync();
        var quotes = (await AsyncExecuter.ToListAsync(quoteQuery.Where(q => q.CreationTime >= prevFrom)))
            .Select(q => new QuoteInfo
            {
                Date     = Utc(q.CreationTime),
                Status   = (int)q.Status,
                PhoneKey = AnalyticsText.CustomerKey(q.Phone, null),
                Name     = q.Name,
                Interest = q.Interest,
                Id       = q.Id,
                Size     = q.EstimatedSize,
                Note     = q.AdminNote,
            }).ToList();

        // ── Customers seen before the history window (for "new customer") ────
        var older = new HashSet<string>();
        foreach (var r in await AsyncExecuter.ToListAsync(invQuery.Where(i => i.InvoiceDate < historyFrom)
                     .Select(i => new { i.CustomerPhone, i.CustomerName })))
            if (AnalyticsText.CustomerKey(r.CustomerPhone, r.CustomerName) is { } k) older.Add(k);
        foreach (var r in await AsyncExecuter.ToListAsync(ordQuery.Where(o => o.OrderDate < historyFrom)
                     .Select(o => new { o.CustomerPhone, o.CustomerName })))
            if (AnalyticsText.CustomerKey(r.CustomerPhone, r.CustomerName) is { } k) older.Add(k);

        // ── Staff names ─────────────────────────────────────────────────────
        var creatorIds = sales.Where(s => s.CreatorId.HasValue).Select(s => s.CreatorId!.Value).Distinct().ToList();
        var userNames  = creatorIds.Count == 0 ? new Dictionary<Guid, string>()
            : (await _users.GetListByIdsAsync(creatorIds)).ToDictionary(
                u => u.Id,
                u => string.Join(' ', new[] { u.Name, u.Surname }.Where(x => !string.IsNullOrWhiteSpace(x))) is { Length: > 0 } full
                     ? full : u.UserName);

        return new AnalyticsSnapshot
        {
            Now      = now,
            Today    = ToDhaka(now).Date,
            From     = from,
            To       = to,
            PrevFrom = prevFrom,
            PrevTo   = from,
            Channel  = channel,
            Sales    = sales,
            Payments = paidRows,
            Products = products,
            Quotes   = quotes,
            OlderCustomerKeys = older,
            UserNames = userNames,
        };
    }

    // ── Time helpers ─────────────────────────────────────────────────────────

    /// <summary>Npgsql legacy mode returns server-local times; normalise everything to UTC.</summary>
    private static DateTime Utc(DateTime d) =>
        d.Kind == DateTimeKind.Utc ? d : DateTime.SpecifyKind(d.ToUniversalTime(), DateTimeKind.Utc);

    private static DateTime ToDhaka(DateTime d) => TimeZoneInfo.ConvertTimeFromUtc(Utc(d), Dhaka);

    /// <summary>UTC instant for the start of a Bangladesh-local date.</summary>
    private static DateTime DhakaDayStartUtc(DateTime localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified), Dhaka);

    private static TimeZoneInfo FindDhaka()
    {
        foreach (var id in new[] { "Asia/Dhaka", "Bangladesh Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { /* try next */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("BST+6", TimeSpan.FromHours(6), "Bangladesh", "Bangladesh");
    }

    // ── Labels ───────────────────────────────────────────────────────────────

    private static string MethodLabel(SalesInvoicePaymentMethod m) => m switch
    {
        SalesInvoicePaymentMethod.Cash         => "Cash",
        SalesInvoicePaymentMethod.Card         => "Card",
        SalesInvoicePaymentMethod.BankTransfer => "Bank transfer",
        SalesInvoicePaymentMethod.Cheque       => "Cheque",
        SalesInvoicePaymentMethod.BKash        => "bKash",
        SalesInvoicePaymentMethod.Nagad        => "Nagad",
        SalesInvoicePaymentMethod.Rocket       => "Rocket",
        _                                      => "Other",
    };

    private static string OrderMethodLabel(OrderPaymentType? t) => t switch
    {
        OrderPaymentType.Cash           => "Cash",
        OrderPaymentType.CreditCard     => "Card",
        OrderPaymentType.DebitCard      => "Card",
        OrderPaymentType.BankTransfer   => "Bank transfer",
        OrderPaymentType.MobileBanking  => "Mobile banking",
        OrderPaymentType.Cheque         => "Cheque",
        OrderPaymentType.Online         => "Online payment",
        OrderPaymentType.CashOnDelivery => "Cash on delivery",
        OrderPaymentType.BKash          => "bKash",
        OrderPaymentType.Nagad          => "Nagad",
        OrderPaymentType.CardEmi        => "Card",
        _                               => "Other",
    };

    private static bool IsDigital(string method) =>
        method is not ("Cash" or "Cash on delivery" or "Cheque" or "Other");
}
