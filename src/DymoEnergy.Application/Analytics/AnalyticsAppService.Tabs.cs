using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Products;
using DymoEnergy.QuoteRequests;

namespace DymoEnergy.Analytics;

public partial class AnalyticsAppService
{
    /// <summary>Products are "running low" when they cover fewer days than this.</summary>
    private const int ReorderWithinDays = 14;
    private const int VelocityDays      = 30;
    private const int SlowAfterDays     = 60;

    // ═════════════════════════════════════════════════════════════════════════
    //  Overview
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(AnalyticsFilterDto input)
    {
        var s       = await LoadAsync(input);
        var period  = s.InPeriod.ToList();
        var prev    = s.InPrevious.ToList();
        var sales   = period.Sum(x => x.Total);
        var quotes  = QuoteStats(s, s.From, s.To);
        var pQuotes = QuoteStats(s, s.PrevFrom, s.PrevTo);
        var kw      = SolarKw(period);
        var pKw     = SolarKw(prev);
        var today   = s.Counted.Where(x => x.LocalDate == s.Today).ToList();
        var open    = s.Open.ToList();
        var overdue = open.Where(x => IsOverdue(x, s)).ToList();

        var dto = new AnalyticsOverviewDto
        {
            Sales          = Math.Round(sales, 2),
            PreviousSales  = Math.Round(prev.Sum(x => x.Total), 2),
            SalesChange    = AnalyticsText.Change(sales, prev.Sum(x => x.Total)),
            MonthlyTarget  = await GetMonthlyTargetAsync(),
            Weeks          = Weeks(s),
            TodaySales     = Math.Round(today.Sum(x => x.Total), 2),
            TodayOrders    = today.Count,
            LatestSales    = s.Counted.OrderByDescending(x => x.LocalCreated).Take(5).Select(x => new AnalyticsLatestSaleDto
            {
                Channel   = x.Channel,
                Summary   = x.Summary,
                Total     = x.Total,
                Date      = x.Date,
                InvoiceId = x.Source == "invoice" ? x.Id : null,
            }).ToList(),
            Orders         = period.Count,
            OrdersChange   = AnalyticsText.Change(period.Count, prev.Count),
            AverageOrder   = period.Count > 0 ? Math.Round(sales / period.Count) : 0,
            QuoteRequests  = quotes.Requests,
            QuotesWon      = quotes.Won,
            QuoteWinRate   = quotes.Rate,
            QuoteWinChange = quotes.Requests > 0 && pQuotes.Requests > 0 ? Math.Round((quotes.Rate - pQuotes.Rate) * 100, 1) : null,
            MoneyStillDue  = Math.Round(open.Sum(x => x.Balance), 2),
            OverdueInvoices = overdue.Count,
            OverdueAmount  = Math.Round(overdue.Sum(x => x.Balance), 2),
            SolarKw        = Math.Round(kw.Kw, 1),
            SolarKwChange  = AnalyticsText.Change(kw.Kw, pKw.Kw),
            PanelsSold     = kw.Panels,
            TopProducts    = AnalyticsText.WithShares(ProductSales(s, period).Take(5)
                                .Select(p => new NamedValueDto { Name = p.Name, Value = Math.Round(p.Sales), Count = (int)p.Qty })),
            Channels       = ChannelBreakdown(period, prev),
        };

        dto.Alerts = OverviewAlerts(s, period, overdue);
        return dto;
    }

    private List<AnalyticsWeekDto> Weeks(AnalyticsSnapshot s)
    {
        var start = ToDhaka(s.From).Date;
        var end   = ToDhaka(AnalyticsSnapshot.Min(s.To, s.Now)).Date;
        var weeks = new List<AnalyticsWeekDto>();
        if (end < start) return weeks;

        double TotalBetween(DateTime a, DateTime b) =>
            s.Counted.Where(x => x.LocalDate >= a && x.LocalDate <= b).Sum(x => x.Total);

        for (var w = start; w <= end && weeks.Count < 6; w = w.AddDays(7))
        {
            var wEnd  = w.AddDays(6) < end ? w.AddDays(6) : end;
            var total = TotalBetween(w, wEnd);
            // Compare with the same number of days just before this chunk
            var days  = (wEnd - w).Days + 1;
            var prev  = TotalBetween(w.AddDays(-days), w.AddDays(-1));
            var inWeek = s.Counted.Where(x => x.LocalDate >= w && x.LocalDate <= wEnd).ToList();
            weeks.Add(new AnalyticsWeekDto
            {
                Label  = w.Month == wEnd.Month ? $"{w.Day}–{wEnd.Day} {wEnd:MMM}" : $"{w:d MMM} – {wEnd:d MMM}",
                From   = w,
                To     = wEnd,
                Sales  = Math.Round(total, 2),
                Orders = inWeek.Count,
                Change = AnalyticsText.Change(total, prev),
                Days   = Enumerable.Range(0, days).Select(i => w.AddDays(i)).Select(d => new AnalyticsDailyDto
                {
                    Date   = d,
                    Sales  = Math.Round(inWeek.Where(x => x.LocalDate == d).Sum(x => x.Total), 2),
                    Orders = inWeek.Count(x => x.LocalDate == d),
                }).ToList(),
            });
        }

        var best = weeks.OrderByDescending(w => w.Sales).FirstOrDefault();
        if (best is { Sales: > 0 } && weeks.Count > 1) best.IsBest = true;
        return weeks;
    }

    private List<AnalyticsAlertDto> OverviewAlerts(AnalyticsSnapshot s, List<SaleRecord> period, List<SaleRecord> overdue)
    {
        var alerts = new List<AnalyticsAlertDto>();

        // 1. A top seller that is out of (or about to run out of) stock
        var ranked   = ProductSales(s, period).ToList();
        var velocity = Velocity(s);
        for (var rank = 0; rank < Math.Min(10, ranked.Count); rank++)
        {
            var product = ranked[rank].Product;
            if (product == null) continue;
            var perDay  = velocity.TryGetValue(product.Id, out var v) ? v : 0;
            var days    = perDay > 0 ? product.Stock / perDay : (double?)null;
            if (product.Stock <= 0 || days < 10)
            {
                alerts.Add(new AnalyticsAlertDto
                {
                    Tone   = product.Stock <= 0 ? "red" : "amber",
                    Title  = product.Stock <= 0 ? $"{product.Name} is out of stock" : $"{product.Name} runs out in ~{Math.Ceiling(days!.Value)} days",
                    Text   = $"Your #{rank + 1} seller — {ranked[rank].Qty:0} sold this period.",
                    Action = "Reorder",
                    Link   = "/products",
                });
                break;
            }
        }

        // 2. Money past its due date
        if (overdue.Count > 0)
        {
            var customers = overdue.Select(x => x.CustomerKey ?? x.Number).Distinct().Count();
            alerts.Add(new AnalyticsAlertDto
            {
                Tone   = "amber",
                Title  = $"Collect {Taka(overdue.Sum(x => x.Balance))} overdue",
                Text   = $"{customers} customer{(customers == 1 ? " is" : "s are")} past their due date.",
                Action = "Send reminders",
                Link   = "/analytics?tab=money",
                Count  = overdue.Count,
            });
        }

        // 3. Leads waiting for a call back
        var waiting = s.Quotes.Where(q => q.Status == (int)QuoteRequestStatus.New && q.Date < s.Now.AddHours(-24)).ToList();
        if (waiting.Count > 0)
        {
            var oldest = (int)Math.Floor((s.Now - waiting.Min(q => q.Date)).TotalDays);
            alerts.Add(new AnalyticsAlertDto
            {
                Tone   = "blue",
                Title  = $"{waiting.Count} quote request{(waiting.Count == 1 ? "" : "s")} waiting",
                Text   = $"Oldest is {oldest} day{(oldest == 1 ? "" : "s")} old. Most losses happen before the first call.",
                Action = "Call back",
                Link   = "/quote-requests",
                Count  = waiting.Count,
            });
        }

        return alerts;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Sales
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AnalyticsSalesDto> GetSalesAsync(AnalyticsFilterDto input)
    {
        var s       = await LoadAsync(input);
        var period  = s.InPeriod.ToList();
        var prev    = s.InPrevious.ToList();
        var sales   = period.Sum(x => x.Total);
        var pSales  = prev.Sum(x => x.Total);
        var avg     = period.Count > 0 ? sales / period.Count : 0;
        var pAvg    = prev.Count > 0 ? pSales / prev.Count : 0;
        var quotes  = QuoteStats(s, s.From, s.To);
        var pQuotes = QuoteStats(s, s.PrevFrom, s.PrevTo);

        // Every day in range, including quiet ones
        var start = ToDhaka(s.From).Date;
        var end   = ToDhaka(AnalyticsSnapshot.Min(s.To, s.Now)).Date;
        var byDay = period.GroupBy(x => x.LocalDate).ToDictionary(g => g.Key, g => g.ToList());
        var daily = new List<AnalyticsDailyDto>();
        for (var d = start; d <= end && daily.Count < 400; d = d.AddDays(1))
        {
            var list = byDay.TryGetValue(d, out var l) ? l : new();
            daily.Add(new AnalyticsDailyDto { Date = d, Sales = Math.Round(list.Sum(x => x.Total), 2), Orders = list.Count });
        }

        // Discounts
        var discounts = new List<NamedValueDto>();
        discounts.AddRange(period.Where(x => x.AdditionalDiscount > 0)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.DiscountNote) ? "Invoice discount" : x.DiscountNote!.Trim())
            .Select(g => new NamedValueDto { Name = g.Key, Value = Math.Round(g.Sum(x => x.AdditionalDiscount)), Count = g.Count() }));
        var lineDisc = period.Where(x => x.Source == "invoice").Sum(x => x.LineDiscount);
        if (lineDisc > 0) discounts.Add(new NamedValueDto { Name = "Line discounts", Value = Math.Round(lineDisc) });
        var vouchers = period.Where(x => x.VoucherAmount > 0).ToList();
        if (vouchers.Count > 0) discounts.Add(new NamedValueDto { Name = "Coupon codes", Value = Math.Round(vouchers.Sum(x => x.VoucherAmount)), Count = vouchers.Count });
        var orderDisc = period.Where(x => x.Source == "order").Sum(x => x.LineDiscount);
        if (orderDisc > 0) discounts.Add(new NamedValueDto { Name = "Online discounts", Value = Math.Round(orderDisc) });
        discounts = AnalyticsText.WithShares(discounts.OrderByDescending(d => d.Value));

        return new AnalyticsSalesDto
        {
            Sales          = Math.Round(sales, 2),
            SalesChange    = AnalyticsText.Change(sales, pSales),
            Orders         = period.Count,
            OrdersChange   = AnalyticsText.Change(period.Count, prev.Count),
            OrdersPerDay   = Math.Round((double)period.Count / s.DaysInPeriod, 1),
            AverageOrder   = Math.Round(avg),
            AverageChange  = AnalyticsText.Change(avg, pAvg),
            QuoteRequests  = quotes.Requests,
            QuotesWon      = quotes.Won,
            QuoteWinRate   = quotes.Rate,
            QuoteWinChange = quotes.Requests > 0 && pQuotes.Requests > 0 ? Math.Round((quotes.Rate - pQuotes.Rate) * 100, 1) : null,
            Daily          = daily,
            ByChannel      = ChannelBreakdown(period, prev),
            ByCategory     = CategoryBreakdown(s, period, prev),
            Heatmap        = Heatmap(period),
            Funnel         = Funnel(s, quotes),
            Salespeople    = period
                .GroupBy(x => x.Source == "order" ? "Online (self-service)"
                            : x.CreatorId.HasValue && s.UserNames.TryGetValue(x.CreatorId.Value, out var n) ? n : "Unknown")
                .Select(g => new AnalyticsSalespersonDto
                {
                    Name    = g.Key,
                    Orders  = g.Count(),
                    Sales   = Math.Round(g.Sum(x => x.Total)),
                    Average = Math.Round(g.Sum(x => x.Total) / g.Count()),
                })
                .OrderByDescending(p => p.Sales).ToList(),
            Discounts      = discounts,
            DiscountTotal  = Math.Round(discounts.Sum(d => d.Value)),
        };
    }

    private static AnalyticsHeatmapDto Heatmap(List<SaleRecord> period)
    {
        // Bangladesh week starts on Saturday
        var days  = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var hours = period.Select(x => x.LocalCreated.Hour).ToList();
        var first = hours.Count > 0 ? Math.Clamp(hours.Min(), 7, 12) : 10;
        var last  = hours.Count > 0 ? Math.Clamp(hours.Max(), 17, 23) : 20;
        var range = Enumerable.Range(first, last - first + 1).ToList();

        var cells = days.Select(d => range.Select(h =>
            period.Count(x => x.LocalCreated.DayOfWeek == d && x.LocalCreated.Hour == h)).ToList()).ToList();

        // Busiest 2-hour window across the week
        string? peak = null;
        if (range.Count >= 2 && period.Count > 0)
        {
            var best = Enumerable.Range(0, range.Count - 1)
                .Select(i => (i, total: cells.Sum(row => row[i] + row[i + 1])))
                .OrderByDescending(t => t.total).First();
            if (best.total > 0) peak = $"{Hour12(range[best.i])}–{Hour12(range[best.i] + 2)}";
        }

        return new AnalyticsHeatmapDto
        {
            Days  = days.Select(d => d.ToString()[..3]).ToList(),
            Hours = range,
            Cells = cells,
            PeakWindow = peak,
        };
    }

    private static string Hour12(int h) => h % 12 == 0 ? (h == 12 ? "12 pm" : "12 am") : $"{h % 12} {(h < 12 ? "am" : "pm")}";

    private static List<AnalyticsFunnelStepDto> Funnel(AnalyticsSnapshot s, QuoteStat q)
    {
        var steps = new[]
        {
            ("Quote requests", q.Requests),
            ("Contacted",      q.Contacted),
            ("Quoted",         q.Quoted),
            ("Won (bought)",   q.Won),
        };
        return steps.Select((st, i) => new AnalyticsFunnelStepDto
        {
            Name  = st.Item1,
            Count = st.Item2,
            Rate  = i == 0 ? null : steps[i - 1].Item2 > 0 ? Math.Round((double)st.Item2 / steps[i - 1].Item2, 3) : 0,
        }).ToList();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Products & stock
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AnalyticsProductsDto> GetProductsAsync(AnalyticsFilterDto input)
    {
        var s        = await LoadAsync(input);
        var period   = s.InPeriod.ToList();
        var velocity = Velocity(s);
        var lastSold = LastSold(s);
        var ranked   = ProductSales(s, period).ToList();
        var total    = ranked.Sum(p => p.Sales);

        double? DaysLeft(ProductInfo p) =>
            velocity.TryGetValue(p.Id, out var v) && v > 0 ? Math.Round(Math.Max(0, p.Stock) / v, 1) : null;

        string StateOf(ProductInfo? p)
        {
            if (p == null) return "none";
            if (p.Stock <= 0) return "out";
            var d = DaysLeft(p);
            return p.Stock <= ProductConsts.LowStockThreshold || d < ReorderWithinDays ? "low" : "ok";
        }

        var stocked  = s.Products.Where(p => p.Stock > 0).ToList();
        var units    = stocked.Sum(p => p.Stock);
        var perDay   = velocity.Values.Sum();
        var slowCut  = s.Now.AddDays(-SlowAfterDays);

        return new AnalyticsProductsDto
        {
            StockValue      = Math.Round(stocked.Sum(p => p.Stock * p.Price)),
            ProductsInStock = stocked.Count,
            UnitsInStock    = units,
            DaysOfStockLeft = perDay > 0 ? Math.Round(units / perDay) : null,
            OutOfStock      = s.Products.Count(p => p.IsActive && p.Stock <= 0),
            RunningLow      = s.Products.Count(p => p.IsActive && p.Stock > 0 && StateOf(p) == "low"),

            BestSellers = ranked.Take(10).Select(r => new AnalyticsProductRowDto
            {
                ProductId  = r.Product?.Id,
                Name       = r.Name,
                Category   = r.Category,
                Sold       = r.Qty,
                Sales      = Math.Round(r.Sales),
                Share      = total > 0 ? Math.Round(r.Sales / total, 4) : 0,
                StockLeft  = r.Product?.Stock,
                DaysLeft   = r.Product != null ? DaysLeft(r.Product) : null,
                StockState = StateOf(r.Product),
            }).ToList(),

            // Cover the next 30 days, rounded up to packs of 5 (shared with the Stock to reorder report)
            Reorder = BuildReorder(s, velocity, limit: 8),

            SlowStock = stocked
                .Where(p => !lastSold.TryGetValue(p.Id, out var d) || d < slowCut)
                .Select(p => new AnalyticsSlowStockDto
                {
                    ProductId = p.Id,
                    Name      = p.Name,
                    Stock     = p.Stock,
                    Value     = Math.Round(p.Stock * p.Price),
                    LastSold  = lastSold.TryGetValue(p.Id, out var d) ? d : null,
                })
                .OrderByDescending(p => p.Value).Take(8).ToList(),

            StockByCategory = AnalyticsText.WithShares(stocked
                .GroupBy(p => p.Category)
                .Select(g => new NamedValueDto { Name = g.Key, Value = Math.Round(g.Sum(p => p.Stock * p.Price)), Count = g.Sum(p => p.Stock) })
                .OrderByDescending(x => x.Value)),
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Money
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AnalyticsMoneyDto> GetMoneyAsync(AnalyticsFilterDto input)
    {
        var s        = await LoadAsync(input);
        var paid     = s.Payments.Where(p => p.Date >= s.From && p.Date <= s.To).ToList();
        var pPaid    = s.Payments.Where(p => p.Date >= s.PrevFrom && p.Date < s.PrevTo).ToList();
        var open     = s.Open.ToList();
        var overdue  = open.Where(x => IsOverdue(x, s)).ToList();
        var refunds  = s.Sales.Where(x => x.Refunded && x.RefundDate >= s.From && x.RefundDate <= s.To).ToList();
        var pRefunds = s.Sales.Where(x => x.Refunded && x.RefundDate >= s.PrevFrom && x.RefundDate < s.PrevTo).ToList();
        var collected = paid.Sum(p => p.Amount);

        var byMethod = AnalyticsText.WithShares(paid.GroupBy(p => p.Method)
            .Select(g => new NamedValueDto { Name = g.Key, Value = Math.Round(g.Sum(p => p.Amount)), Count = g.Count() })
            .OrderByDescending(x => x.Value));

        // Aging of everything still owed
        var today = s.Today;
        int Late(SaleRecord x) => x.DueDate.HasValue ? (today - ToDhaka(x.DueDate.Value).Date).Days : 0;
        var buckets = new[]
        {
            ("Not due yet",     "green", (Func<SaleRecord, bool>)(x => Late(x) <= 0)),
            ("1–30 days late",  "amber", x => Late(x) is >= 1 and <= 30),
            ("31–60 days late", "amber", x => Late(x) is >= 31 and <= 60),
            ("Over 60 days",    "red",   x => Late(x) > 60),
        };

        // Money expected: open, not yet late, by due week
        var expected = new List<NamedValueDto>();
        for (var w = 0; w < 4; w++)
        {
            var a = today.AddDays(w * 7);
            var b = a.AddDays(6);
            expected.Add(new NamedValueDto
            {
                Name  = w == 0 ? "This week" : a.Month == b.Month ? $"{a.Day}–{b.Day} {b:MMM}" : $"{a:d MMM}–{b:d MMM}",
                Value = Math.Round(open.Where(x => x.DueDate.HasValue && ToDhaka(x.DueDate.Value).Date >= a && ToDhaka(x.DueDate.Value).Date <= b).Sum(x => x.Balance)),
                Count = open.Count(x => x.DueDate.HasValue && ToDhaka(x.DueDate.Value).Date >= a && ToDhaka(x.DueDate.Value).Date <= b),
            });
        }
        expected = AnalyticsText.WithShares(expected);

        return new AnalyticsMoneyDto
        {
            Collected       = Math.Round(collected),
            CollectedChange = AnalyticsText.Change(collected, pPaid.Sum(p => p.Amount)),
            StillDue        = Math.Round(open.Sum(x => x.Balance)),
            DueInvoices     = open.Count,
            DueCustomers    = open.Select(x => x.CustomerKey ?? $"#{x.Source}{x.Id}").Distinct().Count(),
            Overdue         = Math.Round(overdue.Sum(x => x.Balance)),
            OverdueInvoices = overdue.Count,
            Refunds         = Math.Round(refunds.Sum(RefundAmount)),
            RefundCount     = refunds.Count,
            RefundsChange   = AnalyticsText.Change(refunds.Sum(RefundAmount), pRefunds.Sum(RefundAmount)),

            ByMethod     = byMethod,
            DigitalShare = collected > 0 ? Math.Round(paid.Where(p => IsDigital(p.Method)).Sum(p => p.Amount) / collected, 3) : 0,
            Aging = buckets.Select(b => new AnalyticsAgingDto
            {
                Name     = b.Item1,
                Tone     = b.Item2,
                Amount   = Math.Round(open.Where(b.Item3).Sum(x => x.Balance)),
                Invoices = open.Count(b.Item3),
            }).ToList(),
            Debtors = open
                .GroupBy(x => x.CustomerKey ?? $"#{x.Source}{x.Id}")
                .Select(g =>
                {
                    var late = g.Max(Late);
                    return new AnalyticsDebtorDto
                    {
                        Name     = g.OrderByDescending(x => x.Date).First().DisplayName,
                        Phone    = g.Select(x => x.Phone).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)),
                        Invoices = g.Count(),
                        Due      = Math.Round(g.Sum(x => x.Balance)),
                        LateDays = late > 0 ? late : null,
                        Note     = late > 0 ? null : g.All(x => x.IsCod) ? "COD" : "not due",
                    };
                })
                // Late first, then by amount — the people to chase today
                .OrderByDescending(d => d.LateDays.HasValue).ThenByDescending(d => d.Due)
                .Take(8).ToList(),
            Expected     = expected,
            LateInvoices = overdue.Where(x => x.Source == "invoice")
                .OrderByDescending(Late)
                .Take(50)
                .Select(x => new AnalyticsReminderDto
                {
                    InvoiceId     = x.Id,
                    InvoiceNumber = x.Number ?? $"#{x.Id}",
                    Name          = x.DisplayName,
                    Phone         = x.Phone,
                    Due           = Math.Round(x.Balance),
                    LateDays      = Late(x),
                }).ToList(),
            VatInSales   = Math.Round(s.InPeriod.Sum(x => x.Tax)),
            VatOnRefunds = Math.Round(refunds.Sum(x => x.Tax)),
        };
    }

    private static double RefundAmount(SaleRecord x) => x.Paid > 0 ? x.Paid : x.Total;

    // ═════════════════════════════════════════════════════════════════════════
    //  Customers
    // ═════════════════════════════════════════════════════════════════════════

    public async Task<AnalyticsCustomersDto> GetCustomersAsync(AnalyticsFilterDto input)
    {
        var s       = await LoadAsync(input);
        var period  = s.InPeriod.ToList();
        var prev    = s.InPrevious.ToList();
        var buyers  = period.Where(x => x.CustomerKey != null).Select(x => x.CustomerKey!).ToHashSet();
        var pBuyers = prev.Where(x => x.CustomerKey != null).Select(x => x.CustomerKey!).ToHashSet();

        var firstSeen = FirstPurchase(s);
        int NewIn(HashSet<string> keys, DateTime from, DateTime to) =>
            keys.Count(k => !s.OlderCustomerKeys.Contains(k) && firstSeen.TryGetValue(k, out var d) && d >= from && d <= to);

        var newCount  = NewIn(buyers, s.From, s.To);
        var pNewCount = NewIn(pBuyers, s.PrevFrom, s.PrevTo);
        var warranty  = WarrantiesEnding(s, 90);
        var overdue   = s.Open.Where(x => IsOverdue(x, s)).ToList();
        var waiting   = s.Quotes.Count(q => q.Status == (int)QuoteRequestStatus.New);

        var followUps = new List<AnalyticsAlertDto>();
        if (warranty.Count > 0)
            followUps.Add(new AnalyticsAlertDto
            {
                Tone = "amber", Count = warranty.Count, Action = "See list", ReportKey = "warranty-ending",
                Title = $"{warranty.Count} warrant{(warranty.Count == 1 ? "y" : "ies")} ending in 90 days",
                Text  = "Offer an extended service plan before they expire.",
            });
        if (overdue.Count > 0)
            followUps.Add(new AnalyticsAlertDto
            {
                Tone = "red", Count = overdue.Select(x => x.CustomerKey ?? x.Number).Distinct().Count(),
                Action = "See dues", ReportKey = "dues-by-age",
                Title = $"{Taka(overdue.Sum(x => x.Balance))} past due",
                Text  = "Customers late on payment — a friendly call usually works.",
            });
        if (waiting > 0)
            followUps.Add(new AnalyticsAlertDto
            {
                Tone = "blue", Count = waiting, Action = "Open", Link = "/quote-requests",
                Title = $"{waiting} new quote request{(waiting == 1 ? "" : "s")}",
                Text  = "Not contacted yet — call back within 24 hours.",
            });

        // Repeat-season nudge: bought around this time last year but not since
        var lastYearFrom = s.From.AddYears(-1);
        var lastYearTo   = s.To.AddYears(-1);
        var seasonal = s.Counted.Where(x => x.Date >= lastYearFrom && x.Date <= lastYearTo && x.CustomerKey != null)
            .Select(x => x.CustomerKey!).Distinct()
            .Count(k => !s.Counted.Any(x => x.CustomerKey == k && x.Date > lastYearTo));
        if (seasonal > 0)
            followUps.Add(new AnalyticsAlertDto
            {
                Tone = "green", Count = seasonal, Action = "See customers", ReportKey = "top-customers",
                Title = $"{seasonal} bought this time last year",
                Text  = "No purchase since — a good moment for a service or upgrade call.",
            });

        return new AnalyticsCustomersDto
        {
            Buyers             = buyers.Count,
            BuyersChange       = AnalyticsText.Change(buyers.Count, pBuyers.Count),
            NewCustomers       = newCount,
            NewChange          = AnalyticsText.Change(newCount, pNewCount),
            Returning          = buyers.Count - newCount,
            ReturningRate      = buyers.Count > 0 ? Math.Round((double)(buyers.Count - newCount) / buyers.Count, 3) : 0,
            WarrantyEndingSoon = warranty.Count,
            Locations = AnalyticsText.WithShares(period
                .GroupBy(x => x.Location ?? "Unknown")
                .Select(g => new NamedValueDto { Name = g.Key, Value = Math.Round(g.Sum(x => x.Total)), Count = g.Count() })
                .OrderByDescending(x => x.Value)
                .Take(7)),
            TopCustomers = TopCustomers(period).Take(8).ToList(),
            FollowUps    = followUps,
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Shared calculations
    // ═════════════════════════════════════════════════════════════════════════

    private bool IsOverdue(SaleRecord x, AnalyticsSnapshot s) =>
        x.DueDate.HasValue && ToDhaka(x.DueDate.Value).Date < s.Today;

    private record ProductSale(string Name, string Category, ProductInfo? Product, double Qty, double Sales);

    /// <summary>Items grouped by catalogue product (or by name for free-text lines), best first.</summary>
    private static IEnumerable<ProductSale> ProductSales(AnalyticsSnapshot s, IEnumerable<SaleRecord> records) =>
        records.SelectMany(r => r.Items)
            .Select(i => (item: i, product: s.FindProduct(i)))
            .GroupBy(t => t.product != null ? $"p{t.product.Id}" : $"n{t.item.Name.ToLowerInvariant()}")
            .Select(g =>
            {
                var first = g.First();
                return new ProductSale(
                    first.product?.Name ?? first.item.Name,
                    first.product?.Category ?? s.CategoryOf(first.item),
                    first.product,
                    g.Sum(t => t.item.Quantity),
                    g.Sum(t => t.item.Total));
            })
            .OrderByDescending(p => p.Sales);


    /// <summary>Units sold per day over the last 30 days, by product id.</summary>
    private static Dictionary<int, double> Velocity(AnalyticsSnapshot s)
    {
        var since = s.Now.AddDays(-VelocityDays);
        return s.Counted.Where(x => x.Date >= since)
            .SelectMany(x => x.Items)
            .Select(i => (item: i, product: s.FindProduct(i)))
            .Where(t => t.product != null)
            .GroupBy(t => t.product!.Id)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.item.Quantity) / VelocityDays);
    }

    private static Dictionary<int, DateTime> LastSold(AnalyticsSnapshot s) =>
        s.Counted.SelectMany(x => x.Items.Select(i => (x.Date, product: s.FindProduct(i))))
            .Where(t => t.product != null)
            .GroupBy(t => t.product!.Id)
            .ToDictionary(g => g.Key, g => g.Max(t => t.Date));

    private static Dictionary<string, DateTime> FirstPurchase(AnalyticsSnapshot s) =>
        s.Counted.Where(x => x.CustomerKey != null)
            .GroupBy(x => x.CustomerKey!)
            .ToDictionary(g => g.Key, g => g.Min(x => x.Date));

    private static (double Kw, int Panels) SolarKw(IEnumerable<SaleRecord> records)
    {
        double watts = 0; var panels = 0;
        foreach (var item in records.SelectMany(r => r.Items))
        {
            if (AnalyticsText.PanelWattage(item.Name) is not { } w) continue;
            watts  += w * item.Quantity;
            panels += (int)item.Quantity;
        }
        return (watts / 1000, panels);
    }

    private record QuoteStat(int Requests, int Contacted, int Quoted, int Won, double Rate);

    /// <summary>A quote is "won" when the same phone number buys on or after the request date.</summary>
    private static QuoteStat QuoteStats(AnalyticsSnapshot s, DateTime from, DateTime to)
    {
        var quotes = s.Quotes.Where(q => q.Date >= from && q.Date <= to).ToList();
        var buys   = s.Counted.Where(x => x.CustomerKey != null && x.CustomerKey.StartsWith("p:"))
                              .GroupBy(x => x.CustomerKey!).ToDictionary(g => g.Key, g => g.Max(x => x.Date));
        var won = quotes.Count(q => q.PhoneKey != null && buys.TryGetValue(q.PhoneKey, out var last) && last >= q.Date.AddDays(-1));

        var contacted = quotes.Count(q => q.Status != (int)QuoteRequestStatus.New);
        var quoted    = quotes.Count(q => q.Status == (int)QuoteRequestStatus.Quoted);
        // Anyone who bought was necessarily contacted and quoted
        quoted    = Math.Max(quoted, won);
        contacted = Math.Max(contacted, quoted);

        return new QuoteStat(quotes.Count, contacted, quoted, won,
            quotes.Count > 0 ? Math.Round((double)won / quotes.Count, 3) : 0);
    }

    private static List<NamedValueDto> ChannelBreakdown(List<SaleRecord> period, List<SaleRecord> prev)
    {
        var prevBy = prev.GroupBy(x => x.Channel).ToDictionary(g => g.Key, g => g.Sum(x => x.Total));
        return AnalyticsText.WithShares(period.GroupBy(x => x.Channel)
            .Select(g => new NamedValueDto
            {
                Name   = g.Key,
                Value  = Math.Round(g.Sum(x => x.Total)),
                Count  = g.Count(),
                Change = AnalyticsText.Change(g.Sum(x => x.Total), prevBy.TryGetValue(g.Key, out var p) ? p : 0),
            })
            .OrderByDescending(x => x.Value));
    }

    private static List<NamedValueDto> CategoryBreakdown(AnalyticsSnapshot s, List<SaleRecord> period, List<SaleRecord> prev)
    {
        Dictionary<string, double> ByCat(IEnumerable<SaleRecord> recs) =>
            recs.SelectMany(r => r.Items).GroupBy(s.CategoryOf).ToDictionary(g => g.Key, g => g.Sum(i => i.Total));
        var cur = ByCat(period);
        var old = ByCat(prev);
        return AnalyticsText.WithShares(cur
            .Select(kv => new NamedValueDto
            {
                Name   = kv.Key,
                Value  = Math.Round(kv.Value),
                Change = AnalyticsText.Change(kv.Value, old.TryGetValue(kv.Key, out var p) ? p : 0),
            })
            .OrderByDescending(x => x.Value));
    }

    private static IEnumerable<AnalyticsTopCustomerDto> TopCustomers(IEnumerable<SaleRecord> records) =>
        records.Where(x => x.CustomerKey != null)
            .GroupBy(x => x.CustomerKey!)
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.Date).First();
                return new AnalyticsTopCustomerDto
                {
                    Name     = latest.DisplayName,
                    Location = g.Select(x => x.Location).FirstOrDefault(l => l != null),
                    Phone    = latest.Phone,
                    Orders   = g.Count(),
                    Spent    = Math.Round(g.Sum(x => x.Total)),
                };
            })
            .OrderByDescending(c => c.Spent);

    private record WarrantyRow(SaleRecord Sale, SaleItem Item, DateTime Ends);

    private List<WarrantyRow> WarrantiesEnding(AnalyticsSnapshot s, int withinDays)
    {
        var until = s.Now.AddDays(withinDays);
        return s.Counted
            .SelectMany(x => x.Items.Select(i => (x, i, end: AnalyticsText.WarrantyEnd(i.Warranty, x.Date))))
            .Where(t => t.end.HasValue && t.end.Value > s.Now && t.end.Value <= until)
            .Select(t => new WarrantyRow(t.x, t.i, t.end!.Value))
            .OrderBy(w => w.Ends).ToList();
    }

    /// <summary>৳1.2 lakh style text for alert sentences.</summary>
    private static string Taka(double v)
    {
        if (v >= 1e7) return $"৳{v / 1e7:0.#} crore";
        if (v >= 1e5) return $"৳{v / 1e5:0.#} lakh";
        return $"৳{v:#,0}";
    }
}
