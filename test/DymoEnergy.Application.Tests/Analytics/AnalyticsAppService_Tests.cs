using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Categories;
using DymoEnergy.Products;
using DymoEnergy.QuoteRequests;
using DymoEnergy.SalesInvoices;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Xunit;

namespace DymoEnergy.Analytics;

public abstract class AnalyticsAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IAnalyticsAppService    _analytics;
    private readonly ISalesInvoiceAppService _invoices;
    private readonly IQuoteRequestAppService _quotes;

    protected AnalyticsAppService_Tests()
    {
        _analytics = GetRequiredService<IAnalyticsAppService>();
        _invoices  = GetRequiredService<ISalesInvoiceAppService>();
        _quotes    = GetRequiredService<IQuoteRequestAppService>();
    }

    private async Task<(int panelId, int batteryId, int slowId)> SeedCatalogueAsync()
    {
        var categories = GetRequiredService<IRepository<Category, int>>();
        var products   = GetRequiredService<IRepository<Product, int>>();
        int panelId = 0, batteryId = 0, slowId = 0;

        await WithUnitOfWorkAsync(async () =>
        {
            var panels    = await categories.InsertAsync(new Category { Name = "Panels" }, autoSave: true);
            var batteries = await categories.InsertAsync(new Category { Name = "Batteries" }, autoSave: true);
            panelId   = (await products.InsertAsync(new Product { Name = "Mono PERC Panel 550W", CategoryId = panels.Id, Price = 17_500, StockQuantity = 100, IsActive = true }, autoSave: true)).Id;
            batteryId = (await products.InsertAsync(new Product { Name = "Lithium Battery 12V 100Ah", CategoryId = batteries.Id, Price = 32_000, StockQuantity = 0, IsActive = true }, autoSave: true)).Id;
            slowId    = (await products.InsertAsync(new Product { Name = "Poly Panel 330W", CategoryId = panels.Id, Price = 9_000, StockQuantity = 22, IsActive = true }, autoSave: true)).Id;
        });
        return (panelId, batteryId, slowId);
    }

    private static AnalyticsFilterDto Last30() => new() { DateFrom = DateTime.UtcNow.AddDays(-30), DateTo = DateTime.UtcNow.AddMinutes(5) };

    [Fact]
    public async Task Should_Aggregate_Every_Tab_From_Real_Records()
    {
        var (panelId, batteryId, slowId) = await SeedCatalogueAsync();

        // Enquiries first — Rafiq later buys, which makes his quote "won"
        await _quotes.CreateAsync(new CreateQuoteRequestDto { Name = "Rafiq Islam", Phone = "+880 1813-088367", Interest = "Residential rooftop" });
        await _quotes.CreateAsync(new CreateQuoteRequestDto { Name = "Sea Breeze Resort", Phone = "01999000111" });

        // Paid showroom sale: 8 panels (4.4 kW) with a warranty ending in ~2 months
        await _invoices.CreateInvoiceDataAsync(new CreateUpdateSalesInvoiceDto
        {
            InvoiceDate = DateTime.UtcNow, Channel = "Showroom POS",
            CustomerName = "Rafiq Islam", CustomerPhone = "01813-088367", BillingAddress = "Halishahar, Chattogram",
            AmountPaid = 140_000, PaymentMethod = SalesInvoicePaymentMethod.BKash,
            Items = new() { new() { ProductId = panelId, ProductName = "Mono PERC Panel 550W", Quantity = 8, UnitPrice = 17_500,
                                    SerialNumbers = "A1, A2", Warranty = "2-month replacement" } },
        });

        // Overdue online-style invoice: battery (now out of stock), nothing paid
        await _invoices.CreateInvoiceDataAsync(new CreateUpdateSalesInvoiceDto
        {
            InvoiceDate = DateTime.UtcNow.AddDays(-20), DueDate = DateTime.UtcNow.AddDays(-5), Channel = "Dhaka showroom",
            CustomerName = "Nabila Bashar", CustomerPhone = "01552904118", BillingAddress = "Dhanmondi, Dhaka",
            AdditionalDiscount = 2_000, DiscountNote = "Bundle promotion",
            Items = new() { new() { ProductId = batteryId, ProductName = "Lithium Battery 12V 100Ah", Quantity = 2, UnitPrice = 32_000 } },
        });


        await _analytics.SetSalesTargetAsync(new SetSalesTargetDto { MonthlyTarget = 700_000 });

        // ── Overview ────────────────────────────────────────────────────────
        var overview = await _analytics.GetOverviewAsync(Last30());
        overview.Sales.ShouldBe(140_000 + 62_000);
        overview.Orders.ShouldBe(2);
        overview.MonthlyTarget.ShouldBe(700_000);
        overview.SolarKw.ShouldBe(4.4);
        overview.PanelsSold.ShouldBe(8);
        overview.OverdueInvoices.ShouldBe(1);
        overview.MoneyStillDue.ShouldBe(62_000);
        overview.Alerts.ShouldContain(a => a.Tone == "red" && a.Title.Contains("Lithium Battery"));
        overview.Channels.Sum(c => c.Share).ShouldBe(1, 0.001);
        overview.Weeks.Sum(w => w.Orders).ShouldBe(2);
        overview.Weeks.SelectMany(w => w.Days).Sum(d => d.Sales).ShouldBe(overview.Weeks.Sum(w => w.Sales), 0.01);
        overview.Weeks.ShouldAllBe(w => w.Days.Count == (w.To - w.From).Days + 1);

        // ── Sales ───────────────────────────────────────────────────────────
        var sales = await _analytics.GetSalesAsync(Last30());
        sales.ByCategory.Single(c => c.Name == "Panels").Value.ShouldBe(140_000);
        sales.Discounts.Single(d => d.Name == "Bundle promotion").Value.ShouldBe(2_000);
        sales.Daily.Sum(d => d.Sales).ShouldBe(202_000);
        sales.Funnel[0].Count.ShouldBeGreaterThanOrEqualTo(2);
        sales.QuotesWon.ShouldBe(1);

        // ── Products ────────────────────────────────────────────────────────
        var products = await _analytics.GetProductsAsync(Last30());
        products.StockValue.ShouldBe(100 * 17_500 + 22 * 9_000);
        products.OutOfStock.ShouldBe(1);
        products.BestSellers.First().Name.ShouldBe("Mono PERC Panel 550W");
        products.BestSellers.Single(b => b.ProductId == batteryId).StockState.ShouldBe("out");
        products.Reorder.ShouldContain(r => r.ProductId == batteryId && r.SuggestedQty > 0);
        products.SlowStock.ShouldContain(r => r.ProductId == slowId);

        // ── Money ───────────────────────────────────────────────────────────
        var money = await _analytics.GetMoneyAsync(Last30());
        money.Collected.ShouldBe(140_000);
        money.ByMethod.Single().Name.ShouldBe("bKash");
        money.Overdue.ShouldBe(62_000);
        money.Aging.Single(a => a.Name == "1–30 days late").Amount.ShouldBe(62_000);
        money.Debtors.First().Name.ShouldBe("Nabila Bashar");
        money.LateInvoices.ShouldHaveSingleItem().LateDays.ShouldBeGreaterThanOrEqualTo(4);

        // ── Customers ───────────────────────────────────────────────────────
        var customers = await _analytics.GetCustomersAsync(Last30());
        customers.Buyers.ShouldBe(2);
        customers.NewCustomers.ShouldBe(2);
        customers.WarrantyEndingSoon.ShouldBe(1);
        customers.Locations.Select(l => l.Name).ShouldBe(new[] { "Chattogram", "Dhaka" });

        // ── Channel filter ──────────────────────────────────────────────────
        (await _analytics.GetOverviewAsync(new AnalyticsFilterDto { DateFrom = Last30().DateFrom, DateTo = Last30().DateTo, Channel = "Showroom POS" }))
            .Sales.ShouldBe(140_000);
        (await _analytics.GetChannelsAsync()).ShouldBe(new List<string> { "Dhaka showroom", "Showroom POS" });
    }

    [Fact]
    public async Task Should_Build_Every_Report_In_Template_Shape()
    {
        var (panelId, batteryId, _) = await SeedCatalogueAsync();
        await _quotes.CreateAsync(new CreateQuoteRequestDto { Name = "Rafiq Islam", Phone = "01813088367", Interest = "Residential rooftop", EstimatedSize = "~5 kW" });
        await _invoices.CreateInvoiceDataAsync(new CreateUpdateSalesInvoiceDto
        {
            InvoiceDate = DateTime.UtcNow, Channel = "Showroom POS", CustomerName = "Rafiq Islam", CustomerPhone = "01813088367",
            AmountPaid = 50_000, PaymentMethod = SalesInvoicePaymentMethod.BKash, AdditionalDiscount = 1_000, DiscountNote = "Bundle promotion",
            DueDate = DateTime.UtcNow.AddDays(-3), TaxInclusive = true,
            Items = new()
            {
                new() { ProductId = panelId,   ProductName = "Mono PERC Panel 550W", Quantity = 2, UnitPrice = 17_500, TaxRate = 5, SerialNumbers = "SN-1, SN-2", Warranty = "2-month replacement" },
                new() { ProductId = batteryId, ProductName = "Lithium Battery 12V 100Ah", Quantity = 1, UnitPrice = 32_000, TaxRate = 5 },
            },
        });

        var keys = new[]
        {
            "sales-by-day", "sales-by-product", "sales-by-salesperson", "quotes-to-sales", "discounts",
            "stock-value", "stock-to-reorder", "slow-stock", "serial-numbers",
            "collections", "dues-by-age", "vat-summary", "refunds",
            "top-customers", "new-customers", "warranty-ending",
        };
        var filter = Last30();

        foreach (var key in keys)
        {
            var r = await _analytics.GetReportAsync(new AnalyticsReportInputDto { Key = key, DateFrom = filter.DateFrom, DateTo = filter.DateTo });
            r.Key.ShouldBe(key);
            r.Category.ShouldNotBeNullOrWhiteSpace();
            r.ReportId.ShouldMatch(@"^[A-Z-]+-\d{4}$");
            r.Kpis.Count.ShouldBe(4, key);
            r.Columns.ShouldNotBeEmpty();
            r.Rows.ShouldAllBe(row => row.Count == r.Columns.Count);
            if (r.Tones != null) r.Tones.Select(t => t.Count).ShouldAllBe(n => n == r.Columns.Count);
            if (r.Totals != null) r.Totals.Count.ShouldBe(r.Columns.Count, key);
        }

        async Task<AnalyticsReportDto> Get(string key) =>
            await _analytics.GetReportAsync(new AnalyticsReportInputDto { Key = key, DateFrom = filter.DateFrom, DateTo = filter.DateTo });

        // Stock value lists the active out-of-stock battery with a red zero
        var stock = await Get("stock-value");
        stock.Rows.Count.ShouldBe(3);
        stock.Tones!.Any(r => r[2] == "red").ShouldBeTrue();
        // Invoices do not move product stock, so the panel count is unchanged
        stock.Totals![4].ShouldBe(100 * 17_500d + 22 * 9_000d);

        // One row per serial number
        (await Get("serial-numbers")).Rows.Select(r => r[0]).ShouldBe(new object?[] { "SN-1", "SN-2" });

        // The quote became a sale → "Won", pointing at the invoice
        var quotes = await Get("quotes-to-sales");
        quotes.Rows.ShouldHaveSingleItem()[5].ShouldBe("Won");
        quotes.Steps!.Count.ShouldBe(4);

        // Promotion discount + overdue due + bKash collection
        (await Get("discounts")).Rows.ShouldContain(r => (string)r[3]! == "Promotion" && (double)r[5]! == 1_000);
        (await Get("dues-by-age")).Rows.ShouldHaveSingleItem()[6].ShouldBe("1–30 days");
        (await Get("collections")).Columns.Select(c => c.Label).ShouldContain("bKash");
    }
}
