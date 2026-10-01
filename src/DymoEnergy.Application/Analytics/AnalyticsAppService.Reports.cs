using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.QuoteRequests;
using Volo.Abp;

namespace DymoEnergy.Analytics;

/// <summary>
/// The 16 ready-made reports. Each one is shaped after its page in the
/// "DymoEnergy report templates" design: category chip, report ID, four KPI tiles,
/// an optional bar section, the table (with per-cell tones) and a closing note.
/// Values stay raw; the admin formats them for screen, PDF and CSV.
/// </summary>
public partial class AnalyticsAppService
{
    private const string CatSales     = "Sales";
    private const string CatProducts  = "Products & stock";
    private const string CatMoney     = "Money";
    private const string CatCustomers = "Customers";

    public async Task<AnalyticsReportDto> GetReportAsync(AnalyticsReportInputDto input)
    {
        var s   = await LoadAsync(input);
        var ctx = new ReportContext(s, PeriodText(s), PreviousLabel(s), ToDhaka(AnalyticsSnapshot.Min(s.To, s.Now)));

        var report = input.Key switch
        {
            "sales-by-day"         => SalesByDay(ctx),
            "sales-by-product"     => SalesByProduct(ctx),
            "sales-by-salesperson" => SalesBySalesperson(ctx),
            "quotes-to-sales"      => QuotesToSales(ctx),
            "discounts"            => Discounts(ctx),
            "stock-value"          => StockValue(ctx),
            "stock-to-reorder"     => StockToReorder(ctx),
            "slow-stock"           => SlowStockReport(ctx),
            "serial-numbers"       => SerialNumbers(ctx),
            "collections"          => Collections(ctx),
            "dues-by-age"          => DuesByAge(ctx),
            "vat-summary"          => VatSummary(ctx),
            "refunds"              => RefundsReport(ctx),
            "top-customers"        => TopCustomersReport(ctx),
            "new-customers"        => NewCustomersReport(ctx),
            "warranty-ending"      => WarrantyEnding(ctx),
            _ => throw new UserFriendlyException($"Unknown report '{input.Key}'."),
        };

        report.Key = input.Key;
        report.ReportId = $"{report.ReportId}-{ctx.End:yyMM}";
        return report;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Sales
    // ═════════════════════════════════════════════════════════════════════════

    private AnalyticsReportDto SalesByDay(ReportContext c)
    {
        var s      = c.S;
        var period = s.InPeriod.ToList();
        var byDay  = period.GroupBy(x => x.LocalDate).ToDictionary(g => g.Key, g => g.ToList());
        var start  = ToDhaka(s.From).Date;
        var t      = new TableBuilder();

        for (var d = start; d <= c.End.Date; d = d.AddDays(1))
        {
            var list  = byDay.TryGetValue(d, out var l) ? l : new();
            var sales = list.Sum(x => x.Total);
            var due   = list.Sum(x => x.Balance);
            t.Row(
                (d, null), (d.ToString("ddd", CultureInfo.InvariantCulture), null), (list.Count, null),
                (Math.Round(list.Sum(Discount)), null), (Math.Round(sales), null), (Math.Round(list.Sum(x => x.Paid)), null),
                // Flag days where a big share is still unpaid
                (Math.Round(due), due > 0 && due > sales * 0.15 ? "amber" : null),
                (Math.Round(list.Sum(x => x.Tax)), null));
        }

        var total     = period.Sum(x => x.Total);
        var collected = period.Sum(x => x.Paid);
        var best      = byDay.OrderByDescending(kv => kv.Value.Sum(x => x.Total)).FirstOrDefault();

        return t.Into(new AnalyticsReportDto
        {
            Title = "Sales by day", Subtitle = c.Range, Category = CatSales, ReportId = "SALES-DAY",
            Kpis =
            {
                Kpi("Sales", Math.Round(total), "money", tone: "green", change: AnalyticsText.Change(total, s.InPrevious.Sum(x => x.Total)), sub: $"vs {c.Previous}"),
                Kpi("Invoices", period.Count, sub: $"{(double)period.Count / s.DaysInPeriod:0.#} a day"),
                Kpi("Collected", Math.Round(collected), "money", sub: total > 0 ? $"{collected / total:P0} of sales" : null),
                best.Value is { Count: > 0 }
                    ? Kpi("Best day", best.Key.ToString("d MMM", CultureInfo.InvariantCulture), "text", sub: Taka(best.Value.Sum(x => x.Total)))
                    : Kpi("Best day", "—", "text"),
            },
            Columns =
            {
                Col("Date", "date"), Col("Day", muted: true), Col("Invoices", "number"), Col("Discount", "money"),
                Col("Sales", "money", bold: true), Col("Collected", "money"), Col("Still due", "money"), Col("VAT incl.", "money"),
            },
            Note = "Sales include VAT and are after discounts.",
        }, sumFrom: 2);
    }

    private AnalyticsReportDto SalesByProduct(ReportContext c)
    {
        var s      = c.S;
        var ranked = ProductSales(s, s.InPeriod).ToList();
        var total  = ranked.Sum(p => p.Sales);
        var units  = ranked.Sum(p => p.Qty);
        var t      = new TableBuilder();

        var rank = 0;
        foreach (var p in ranked)
            t.Row((++rank, "muted"), (p.Name, null), (p.Category, null), (p.Product?.Sku ?? SkuOf(s, p.Name), null),
                  (p.Qty, null), (p.Qty > 0 ? Math.Round(p.Sales / p.Qty) : 0, null), (Math.Round(p.Sales), null),
                  (total > 0 ? Math.Round(p.Sales / total, 4) : 0, null));

        var top = ranked.FirstOrDefault();
        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Sales by product", Subtitle = c.Range, Category = CatSales, ReportId = "SALES-PRODUCT",
            Kpis =
            {
                Kpi("Products sold", ranked.Count, sub: $"of {s.Products.Count} in the catalogue"),
                Kpi("Units", units),
                Kpi("Sales", Math.Round(total), "money", tone: "green"),
                Kpi("Top product", top?.Name ?? "—", "text", sub: top != null && total > 0 ? $"{top.Sales / total:P0} of sales" : null),
            },
            Columns =
            {
                Col("#", "number", muted: true), Col("Product", bold: true), Col("Category"), Col("SKU", mono: true),
                Col("Units", "number"), Col("Avg price", "money"), Col("Sales", "money", bold: true), Col("Share", "share"),
            },
        });
        dto.Totals = new List<object?> { null, null, null, null, units, null, Math.Round(total), ranked.Count > 0 ? 1d : 0d };
        return dto;
    }

    private AnalyticsReportDto SalesBySalesperson(ReportContext c)
    {
        var s      = c.S;
        var period = s.InPeriod.ToList();
        var quotes = QuoteStats(s, s.From, s.To);
        var groups = period
            .GroupBy(x => x.Source == "order" ? "Online (self-service)"
                        : x.CreatorId.HasValue && s.UserNames.TryGetValue(x.CreatorId.Value, out var n) ? n : "Unknown")
            .Select(g => new
            {
                Name     = g.Key,
                Channel  = g.GroupBy(x => x.Channel).OrderByDescending(x => x.Count()).First().Key,
                Invoices = g.Count(),
                Items    = g.Sum(x => x.Items.Sum(i => i.Quantity)),
                Sales    = g.Sum(x => x.Total),
                Discount = g.Sum(Discount),
            })
            .OrderByDescending(g => g.Sales).ToList();
        var total = groups.Sum(g => g.Sales);

        var t = new TableBuilder();
        foreach (var g in groups)
            t.Row((g.Name, null), (g.Channel, null), (g.Invoices, null), (g.Items, null), (Math.Round(g.Sales), null),
                  (Math.Round(g.Sales / Math.Max(1, g.Invoices)), null), (Math.Round(g.Discount), null));

        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Sales by salesperson", Subtitle = c.Range, Category = CatSales, ReportId = "SALES-STAFF",
            Kpis =
            {
                Kpi("People & channels", groups.Count),
                Kpi("Invoices", period.Count),
                Kpi("Sales", Math.Round(total), "money", tone: "green"),
                Kpi("Quotes won", quotes.Won, sub: $"of {quotes.Requests} requests"),
            },
            Columns =
            {
                Col("Salesperson", bold: true), Col("Counter / channel", muted: true), Col("Invoices", "number"), Col("Items", "number"),
                Col("Sales", "money", bold: true), Col("Avg invoice", "money"), Col("Discounts given", "money"),
            },
            BarsTitle = "Share of sales",
            Bars = AnalyticsText.WithShares(groups.Select(g => new NamedValueDto { Name = g.Name, Value = Math.Round(g.Sales) })),
            Note = "Salesperson = the staff member who created the invoice. Online orders have no salesperson.",
        }, sumFrom: 2);
        if (dto.Totals != null) dto.Totals[5] = period.Count > 0 ? Math.Round(total / period.Count) : 0;
        return dto;
    }

    private AnalyticsReportDto QuotesToSales(ReportContext c)
    {
        var s      = c.S;
        var quotes = s.Quotes.Where(q => q.Date >= s.From && q.Date <= s.To).OrderByDescending(q => q.Date).ToList();
        var stat   = QuoteStats(s, s.From, s.To);
        var t      = new TableBuilder();
        double wonValue = 0;
        var stillOpen = 0;

        foreach (var q in quotes)
        {
            // The first purchase by the same phone on/after the request is the "won" sale
            var sale = q.PhoneKey == null ? null
                : s.Counted.Where(x => x.CustomerKey == q.PhoneKey && x.Date >= q.Date.AddDays(-1)).OrderBy(x => x.Date).FirstOrDefault();
            string status, tone, next;
            if (sale != null)                                 { status = "Won";  tone = "green"; next = sale.Number ?? "Bought"; wonValue += sale.Total; }
            else if (q.Status == (int)QuoteRequestStatus.Closed) { status = "Lost"; tone = "red"; next = q.Note ?? "Closed"; }
            else
            {
                status = "Open"; tone = "blue"; stillOpen++;
                next = q.Note ?? q.Status switch
                {
                    (int)QuoteRequestStatus.New       => "Call back",
                    (int)QuoteRequestStatus.Contacted => "Send the quote",
                    _                                 => "Follow up the quote",
                };
            }

            var system = string.Join(" · ", new[] { q.Size, q.Interest }.Where(x => !string.IsNullOrWhiteSpace(x)));
            t.Row(($"Q-{q.Id}", null), (ToDhaka(q.Date).Date, null), (q.Name, null), (system.Length > 0 ? system : "—", null),
                  (sale != null ? Math.Round(sale.Total) : null, null), (status, tone), (next, null));
        }

        string Step(int count, int of) => of > 0 ? $"{count} · {(double)count / of:P0}" : count.ToString(CultureInfo.InvariantCulture);

        return t.Into(new AnalyticsReportDto
        {
            Title = "Quotes to sales", Subtitle = c.Range, Category = CatSales, ReportId = "QUOTES",
            Kpis =
            {
                Kpi("Requests", stat.Requests),
                Kpi("Won", stat.Won, tone: "green", sub: $"{stat.Rate:P0} of requests"),
                Kpi("Value won", Math.Round(wonValue), "money"),
                Kpi("Still open", stillOpen, tone: stillOpen > 0 ? "amber" : "neutral", sub: "not bought yet"),
            },
            Steps = new()
            {
                Kpi("Quote requests", stat.Requests.ToString(CultureInfo.InvariantCulture), "text"),
                Kpi("Contacted", Step(stat.Contacted, stat.Requests), "text", tone: "green"),
                Kpi("Quoted", Step(stat.Quoted, stat.Contacted), "text", tone: "green"),
                Kpi("Won (bought)", Step(stat.Won, stat.Quoted), "text", tone: "dark"),
            },
            TableTitle = "Quotes this period",
            Columns =
            {
                Col("Quote", mono: true, bold: true), Col("Date", "date"), Col("Customer"), Col("System", muted: true),
                Col("Value", "money", bold: true), Col("Status", "chip"), Col("Next step / reason", muted: true),
            },
            Note = "Won = the same phone number bought after asking for a quote. Most quotes are lost before the first call — call every new request within 24 hours.",
            NoteTone = "amber",
        });
    }

    private AnalyticsReportDto Discounts(ReportContext c)
    {
        var s      = c.S;
        var period = s.InPeriod.OrderByDescending(x => x.Date).ToList();
        var t      = new TableBuilder();
        double promo = 0, counter = 0, coupons = 0;

        foreach (var x in period)
        {
            var who = x.Source == "order" ? "Online"
                    : x.CreatorId.HasValue && s.UserNames.TryGetValue(x.CreatorId.Value, out var n) ? n : "—";
            void Add(string type, string tone, string reason, double amount, string by)
            {
                t.Row((x.LocalDate, null), (x.Number, null), (x.DisplayName, null), (type, tone), (reason, null), (Math.Round(amount), null), (by, null));
            }

            if (x.AdditionalDiscount > 0) { promo += x.AdditionalDiscount; Add("Promotion", "green", x.DiscountNote ?? "Invoice discount", x.AdditionalDiscount, who); }
            if (x.Source == "invoice" && x.LineDiscount > 0) { counter += x.LineDiscount; Add("Counter", "amber", "Line discount", x.LineDiscount, who); }
            if (x.VoucherAmount > 0) { coupons += x.VoucherAmount; Add("Coupon", "blue", x.Voucher ?? "Coupon", x.VoucherAmount, "Automatic"); }
            if (x.Source == "order" && x.LineDiscount > 0) { promo += x.LineDiscount; Add("Promotion", "green", "Online price discount", x.LineDiscount, "Automatic"); }
        }

        var total = promo + counter + coupons;
        var sales = period.Sum(x => x.Total);
        return t.Into(new AnalyticsReportDto
        {
            Title = "Discounts given", Subtitle = c.Range, Category = CatSales, ReportId = "DISCOUNTS",
            Kpis =
            {
                Kpi("Total discount", Math.Round(total), "money", tone: "green", sub: sales > 0 ? $"{total / sales:P1} of sales" : null),
                Kpi("Promotions", Math.Round(promo), "money"),
                Kpi("Counter discounts", Math.Round(counter), "money", tone: counter > 0 ? "amber" : "neutral", sub: "line discounts at the counter"),
                Kpi("Coupons", Math.Round(coupons), "money"),
            },
            Columns =
            {
                Col("Date", "date"), Col("Invoice", mono: true), Col("Customer"), Col("Type", "chip"),
                Col("Promotion / reason"), Col("Discount", "money", bold: true), Col("Given by", muted: true),
            },
            Note = "Promotions are invoice-level discounts (with their note); counter discounts are per-line % discounts.",
        }, sumFrom: 5);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Products & stock
    // ═════════════════════════════════════════════════════════════════════════

    private AnalyticsReportDto StockValue(ReportContext c)
    {
        var s    = c.S;
        var rows = s.Products.Where(p => p.Stock > 0 || p.IsActive)
                             .OrderBy(p => p.Category).ThenByDescending(p => p.Stock * p.Price).ToList();
        var t = new TableBuilder();
        foreach (var p in rows)
        {
            var stock = Math.Max(0, p.Stock);
            t.Row((p.Name, null), (p.Category, null), (stock, stock == 0 ? "red" : null), (p.Price, null), (Math.Round(stock * p.Price), null));
        }

        var stocked = rows.Where(p => p.Stock > 0).ToList();
        var outCount = rows.Count(p => p.Stock <= 0);
        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Stock value", Subtitle = $"On {c.End:d MMMM yyyy, HH:mm} · at selling price", Category = CatProducts, ReportId = "STOCK-VALUE",
            Kpis =
            {
                Kpi("Stock value", Math.Round(stocked.Sum(p => p.Stock * p.Price)), "money", tone: "green", sub: "at selling price"),
                Kpi("Units", stocked.Sum(p => p.Stock)),
                Kpi("Products in stock", stocked.Count, sub: $"of {rows.Count} active products"),
                Kpi("Out of stock", outCount, tone: outCount > 0 ? "red" : "neutral"),
            },
            Columns = { Col("Product", bold: true), Col("Category", muted: true), Col("In stock", "number"), Col("Price", "money"), Col("Value", "money") },
            Note = "Value uses the current selling price — cost price isn't recorded yet. Stock is one figure per product (no per-showroom split).",
        });
        dto.Totals = new List<object?> { null, null, stocked.Sum(p => p.Stock), null, Math.Round(stocked.Sum(p => p.Stock * p.Price)) };
        return dto;
    }

    private AnalyticsReportDto StockToReorder(ReportContext c)
    {
        var s        = c.S;
        var velocity = Velocity(s);
        var rows     = BuildReorder(s, velocity, limit: int.MaxValue);
        var t        = new TableBuilder();

        foreach (var r in rows)
        {
            var p = s.Products.First(x => x.Id == r.ProductId);
            var (runsOut, tone) = r.Stock <= 0 ? ("Out now", "red")
                : r.DaysLeft is { } d ? ($"{Math.Ceiling(d)} days", d <= 10 ? "amber" : null) : ("—", null);
            t.Row((r.Name, null), (r.Stock, r.Stock <= 0 ? "red" : "amber"), (r.SoldLast30, null), (runsOut, tone),
                  (r.SuggestedQty, null), (p.Vendor ?? "—", null), (Math.Round(r.SuggestedQty * p.Price), null));
        }

        var outNow    = rows.Count(r => r.Stock <= 0);
        var suppliers = rows.Select(r => s.Products.First(x => x.Id == r.ProductId).Vendor).Where(v => v != null).Distinct().Count();
        var value     = rows.Sum(r => r.SuggestedQty * s.Products.First(x => x.Id == r.ProductId).Price);
        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Stock to reorder", Subtitle = $"Items that will run out in the next {ReorderWithinDays} days · based on the last 30 days of sales",
            Category = CatProducts, ReportId = "REORDER",
            Kpis =
            {
                Kpi("Items to reorder", rows.Count, tone: rows.Count > 0 ? "red" : "neutral", sub: outNow > 0 ? $"{outNow} already out" : null),
                Kpi("Suggested order", $"{rows.Sum(r => r.SuggestedQty):#,0} units", "text"),
                Kpi("Estimated value", Math.Round(value), "money", tone: "green", sub: "at selling price"),
                Kpi("Suppliers", suppliers, sub: suppliers == 0 ? "add vendors on products" : "on the product records"),
            },
            Columns =
            {
                Col("Product", bold: true), Col("In stock", "number"), Col("Sold in 30 days", "number"), Col("Runs out in"),
                Col("Suggested qty", "number", bold: true), Col("Supplier", muted: true), Col("Est. value", "money"),
            },
            Note = "Suggested quantity covers the next 30 days of sales, rounded up to packs of 5. Load-shedding season (April–June) needs about double for IPS and batteries.",
            NoteTone = "amber",
        });
        dto.Totals = new List<object?> { null, null, null, null, rows.Sum(r => r.SuggestedQty), null, Math.Round(value) };
        return dto;
    }

    private AnalyticsReportDto SlowStockReport(ReportContext c)
    {
        var s        = c.S;
        var lastSold = LastSold(s);
        var cut      = s.Now.AddDays(-SlowAfterDays);
        var stocked  = s.Products.Where(p => p.Stock > 0).ToList();
        var slow     = stocked.Where(p => !lastSold.TryGetValue(p.Id, out var d) || d < cut)
                              .OrderByDescending(p => p.Stock * p.Price).ToList();
        var t = new TableBuilder();

        foreach (var p in slow)
        {
            DateTime? last = lastSold.TryGetValue(p.Id, out var d) ? ToDhaka(d) : null;
            int? since     = last.HasValue ? (c.End.Date - last.Value.Date).Days : null;
            var value      = p.Stock * p.Price;
            var suggestion = !since.HasValue ? "No sale on record — check it is still sellable"
                           : since > 90       ? "Return to supplier if allowed"
                           : value >= 100_000 ? "Bundle with a fast seller at a small discount"
                           :                    "Promote in an offer";
            t.Row((p.Name, null), (p.Stock, null), (Math.Round(value), null), (last, null), (since, "amber"), (suggestion, null));
        }

        var tied   = slow.Sum(p => p.Stock * p.Price);
        var all    = stocked.Sum(p => p.Stock * p.Price);
        var oldest = slow.Select(p => (p, d: lastSold.TryGetValue(p.Id, out var x) ? x : (DateTime?)null))
                         .OrderBy(t2 => t2.d ?? DateTime.MinValue).FirstOrDefault();
        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Slow stock", Subtitle = $"Items with no sale in the last {SlowAfterDays} days · on {c.End:d MMMM yyyy}",
            Category = CatProducts, ReportId = "SLOW",
            Kpis =
            {
                Kpi("Slow items", slow.Count, tone: slow.Count > 0 ? "amber" : "neutral"),
                Kpi("Money tied up", Math.Round(tied), "money", tone: tied > 0 ? "amber" : "neutral", sub: "at selling price"),
                Kpi("Share of stock", all > 0 ? tied / all : 0, "percent", sub: "of total stock value"),
                oldest.p != null
                    ? Kpi("Oldest", oldest.d.HasValue ? $"{(c.End.Date - ToDhaka(oldest.d.Value).Date).Days} days" : "never sold", "text", sub: oldest.p.Name)
                    : Kpi("Oldest", "—", "text"),
            },
            Columns = { Col("Product", bold: true), Col("Qty", "number"), Col("Value", "money"), Col("Last sold", "date"), Col("Days since", "number"), Col("Suggestion", muted: true) },
            Note = "Clearing slow stock frees cash for fast sellers. Create a promotion from Marketing › Promotions.",
        });
        dto.Totals = new List<object?> { null, slow.Sum(p => p.Stock), Math.Round(tied), null, null, null };
        return dto;
    }

    private AnalyticsReportDto SerialNumbers(ReportContext c)
    {
        var s     = c.S;
        var sales = s.Sales.Where(x => (x.Counted || x.Refunded) && x.Date >= s.From && x.Date <= s.To)
                           .OrderByDescending(x => x.Date).ToList();
        var t = new TableBuilder();
        int total = 0, withWarranty = 0, returned = 0;
        double missing = 0;

        foreach (var x in sales)
        foreach (var i in x.Items)
        {
            var serials = SplitSerials(i.Serials);
            // Catalogue products sold without enough serials (services / cables are exempt)
            if (s.FindProduct(i) != null && !AnalyticsText.IsService(i.Name) && serials.Count < i.Quantity && IsSerialised(i.Name))
                missing += i.Quantity - serials.Count;

            var ends = AnalyticsText.WarrantyEnd(i.Warranty, x.Date);
            foreach (var serial in serials)
            {
                total++;
                if (ends.HasValue) withWarranty++;
                if (x.Refunded) returned++;
                t.Row((serial, null), (i.Name, null), (x.Number, null), (x.DisplayName, null), (x.LocalDate, null),
                      (ends.HasValue ? ToDhaka(ends.Value).ToString("MMM yyyy", CultureInfo.InvariantCulture) : i.Warranty ?? "—", null),
                      (x.Refunded ? "Returned" : "Sold", x.Refunded ? "red" : "green"));
            }
        }

        return t.Into(new AnalyticsReportDto
        {
            Title = "Serial numbers", Subtitle = c.Range, Category = CatProducts, ReportId = "SERIALS",
            Kpis =
            {
                Kpi("Serials sold", total, tone: "green"),
                Kpi("With warranty", withWarranty, sub: "warranty recorded on the invoice"),
                Kpi("Returned", returned, tone: returned > 0 ? "amber" : "neutral", sub: "on refunded sales"),
                Kpi("Missing serials", (int)missing, tone: missing > 0 ? "red" : "neutral",
                    sub: missing > 0 ? "units sold without a serial" : "all sales scanned"),
            },
            Columns =
            {
                Col("Serial number", mono: true, bold: true), Col("Product"), Col("Invoice", mono: true), Col("Customer"),
                Col("Sold on", "dateyear"), Col("Warranty until", muted: true), Col("Status", "chip"),
            },
            Note = "Missing serials counts panels, inverters, batteries and controllers sold without a serial number on the invoice line.",
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Money
    // ═════════════════════════════════════════════════════════════════════════

    private AnalyticsReportDto Collections(ReportContext c)
    {
        var s       = c.S;
        var paid    = s.Payments.Where(p => p.Date >= s.From && p.Date <= s.To).ToList();
        var prev    = s.Payments.Where(p => p.Date >= s.PrevFrom && p.Date < s.PrevTo).Sum(p => p.Amount);
        var total   = paid.Sum(p => p.Amount);
        var methods = paid.GroupBy(p => p.Method).OrderByDescending(g => g.Sum(p => p.Amount)).Select(g => g.Key).ToList();

        // Keep the table to about 16 rows: days, 2-day groups, or weeks
        var start = ToDhaka(s.From).Date;
        var days  = (c.End.Date - start).Days + 1;
        var step  = days <= 16 ? 1 : days <= 32 ? 2 : 7;
        var t     = new TableBuilder();

        for (var d = start; d <= c.End.Date; d = d.AddDays(step))
        {
            var end   = d.AddDays(step - 1) < c.End.Date ? d.AddDays(step - 1) : c.End.Date;
            var inBin = paid.Where(p => ToDhaka(p.Date).Date >= d && ToDhaka(p.Date).Date <= end).ToList();
            var label = step == 1 ? d.ToString("dd MMM", CultureInfo.InvariantCulture)
                      : d.Month == end.Month ? $"{d:dd}–{end:dd MMM}" : $"{d:dd MMM} – {end:dd MMM}";
            var cells = new List<(object?, string?)> { (label, null) };
            cells.AddRange(methods.Select(m => ((object?)Math.Round(inBin.Where(p => p.Method == m).Sum(p => p.Amount)), (string?)null)));
            cells.Add((Math.Round(inBin.Sum(p => p.Amount)), null));
            t.Row(cells.ToArray());
        }

        var byDay   = paid.GroupBy(p => ToDhaka(p.Date).Date).OrderByDescending(g => g.Sum(p => p.Amount)).FirstOrDefault();
        var cash    = paid.Where(p => p.Method is "Cash" or "Cash on delivery").Sum(p => p.Amount);
        var digital = paid.Where(p => IsDigital(p.Method)).Sum(p => p.Amount);

        var columns = new List<AnalyticsReportColumnDto> { Col(step == 1 ? "Day" : step == 2 ? "Days" : "Week") };
        columns.AddRange(methods.Select(m => Col(m, "money")));
        columns.Add(Col("Total", "money", bold: true));

        return t.Into(new AnalyticsReportDto
        {
            Title = "Collections", Subtitle = $"{c.Range} · money received, by method", Category = CatMoney, ReportId = "COLLECTIONS",
            Kpis =
            {
                Kpi("Collected", Math.Round(total), "money", tone: "green", change: AnalyticsText.Change(total, prev), sub: $"vs {c.Previous}"),
                Kpi("Digital", total > 0 ? digital / total : 0, "percent", sub: "mobile banking, card, bank"),
                Kpi("Cash", Math.Round(cash), "money", sub: "cash and cash on delivery"),
                byDay != null
                    ? Kpi("Largest day", byDay.Key.ToString("d MMM", CultureInfo.InvariantCulture), "text", sub: Taka(byDay.Sum(p => p.Amount)))
                    : Kpi("Largest day", "—", "text"),
            },
            BarsTitle  = "By method",
            Bars       = AnalyticsText.WithShares(methods.Select(m => new NamedValueDto { Name = m, Value = Math.Round(paid.Where(p => p.Method == m).Sum(p => p.Amount)) })),
            TableTitle = step == 1 ? "By day" : step == 2 ? "By day (2-day groups)" : "By week",
            Columns    = columns,
        }, sumFrom: 1);
    }

    private AnalyticsReportDto DuesByAge(ReportContext c)
    {
        var s = c.S;
        int Late(SaleRecord x) => x.DueDate.HasValue ? (s.Today - ToDhaka(x.DueDate.Value).Date).Days : 0;
        var open = s.Open.OrderByDescending(Late).ThenByDescending(x => x.Balance).ToList();
        var t    = new TableBuilder();

        foreach (var x in open)
        {
            var late = Late(x);
            var (group, tone) = late <= 0 ? ("Not due", "green") : late <= 30 ? ("1–30 days", "amber") : late <= 60 ? ("31–60 days", "amber") : ("Over 60 days", "red");
            t.Row((x.DisplayName, null), (x.Number, null), (x.LocalDate, null),
                  (x.DueDate.HasValue ? ToDhaka(x.DueDate.Value).ToString("d MMM", CultureInfo.InvariantCulture) : x.IsCod ? "on delivery" : "—", null),
                  (Math.Round(x.Balance), null), (late > 0 ? late : null, late > 60 ? "red" : "amber"), (group, tone));
        }

        double Sum(Func<int, bool> f) => Math.Round(open.Where(x => f(Late(x))).Sum(x => x.Balance));
        int    Cnt(Func<int, bool> f) => open.Count(x => f(Late(x)));

        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Dues by age", Subtitle = $"Money customers still owe · on {ToDhaka(s.Now):d MMMM yyyy}", Category = CatMoney, ReportId = "DUES",
            Kpis =
            {
                Kpi("Total due", Math.Round(open.Sum(x => x.Balance)), "money", tone: "amber", sub: $"{open.Count} invoices"),
                Kpi("Not due yet", Sum(l => l <= 0), "money", tone: "green", sub: $"{Cnt(l => l <= 0)} invoices"),
                Kpi("Late 1–60 days", Sum(l => l is >= 1 and <= 60), "money", tone: "amber", sub: $"{Cnt(l => l is >= 1 and <= 60)} invoices"),
                Kpi("Over 60 days", Sum(l => l > 60), "money", tone: "red", sub: $"{Cnt(l => l > 60)} invoices"),
            },
            Columns =
            {
                Col("Customer", bold: true), Col("Invoice", mono: true), Col("Invoice date", "date"), Col("Due date"),
                Col("Amount due", "money", bold: true), Col("Days late", "number"), Col("Group", "chip"),
            },
            Note = "Send reminders from Analytics › Money. Call anyone over 30 days late; hold new credit for customers over 60 days.",
            NoteTone = "amber",
        });
        dto.Totals = new List<object?> { null, null, null, null, Math.Round(open.Sum(x => x.Balance)), null, null };
        return dto;
    }

    private AnalyticsReportDto VatSummary(ReportContext c)
    {
        var s       = c.S;
        var period  = s.InPeriod.ToList();
        var refunds = s.Sales.Where(x => x.Refunded && x.RefundDate >= s.From && x.RefundDate <= s.To).ToList();
        var t       = new TableBuilder();

        var byCat = period.SelectMany(x => x.Items).GroupBy(s.CategoryOf)
            .Select(g => (Name: g.Key, Gross: g.Sum(i => i.Total), Vat: g.Sum(i => i.Tax)))
            .OrderByDescending(g => g.Gross).ToList();
        foreach (var g in byCat)
            t.Row((g.Name, null), (Math.Round(g.Gross), null), (Math.Round(g.Gross - g.Vat), null),
                  (g.Gross - g.Vat > 0 ? Math.Round(g.Vat / (g.Gross - g.Vat), 3) : 0, null), (Math.Round(g.Vat), null));

        var rGross = refunds.Sum(x => x.Total);
        var rVat   = refunds.Sum(x => x.Tax);
        if (refunds.Count > 0)
            t.Row(("Refunds & returns", "red"), (-Math.Round(rGross), "red"), (-Math.Round(rGross - rVat), "red"),
                  (rGross - rVat > 0 ? Math.Round(rVat / (rGross - rVat), 3) : 0, "red"), (-Math.Round(rVat), "red"));

        var gross = byCat.Sum(g => g.Gross);
        var vat   = byCat.Sum(g => g.Vat);
        var rate  = gross - vat > 0 ? vat / (gross - vat) : 0;
        var numbers = period.Where(x => x.Source == "invoice" && x.Number != null).Select(x => x.Number!).OrderBy(n => n).ToList();
        var cancelled = s.Sales.Count(x => x.Source == "invoice" && !x.Counted && !x.Refunded && x.Date >= s.From && x.Date <= s.To);

        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "VAT summary (Mushak)", Subtitle = $"{c.RangeShort} · for the monthly VAT return", Category = CatMoney, ReportId = "VAT",
            Kpis =
            {
                Kpi("Sales incl. VAT", Math.Round(gross), "money"),
                Kpi("VAT included", Math.Round(vat), "money", tone: "green", sub: $"at {rate:P1} on average"),
                Kpi("VAT on refunds", -Math.Round(rVat), "money", tone: rVat > 0 ? "amber" : "neutral", sub: $"{refunds.Count} refund{(refunds.Count == 1 ? "" : "s")}"),
                Kpi("Net VAT", Math.Round(vat - rVat), "money", tone: "green", sub: "to declare"),
            },
            Columns =
            {
                Col("Category", bold: true), Col("Sales incl. VAT", "money"), Col("Sales before VAT", "money"),
                Col("VAT rate", "percent"), Col("VAT included", "money"),
            },
            TotalsLabel = "Net",
            Facts = new()
            {
                new() { Title = "Invoices", Text = numbers.Count > 0
                    ? $"{numbers.First()} to {numbers.Last()} · {numbers.Count} invoices · {cancelled} cancelled"
                    : "No invoices in this period" },
                new() { Title = "Tax period", Text = c.RangeShort },
            },
            Note = "VAT comes from each invoice line's VAT rate, before invoice-level discounts. Check the figures with your accountant before filing the Mushak return.",
            NoteTone = "amber",
        });
        dto.Totals = new List<object?> { null, Math.Round(gross - rGross), Math.Round((gross - vat) - (rGross - rVat)), null, Math.Round(vat - rVat) };
        return dto;
    }

    private AnalyticsReportDto RefundsReport(ReportContext c)
    {
        var s       = c.S;
        var refunds = s.Sales.Where(x => x.Refunded && x.RefundDate >= s.From && x.RefundDate <= s.To)
                             .OrderByDescending(x => x.RefundDate).ToList();
        var t = new TableBuilder();
        foreach (var x in refunds)
            t.Row((x.Number, null), (ToDhaka(x.RefundDate!.Value).Date, null), (x.DisplayName, null),
                  (string.Join(", ", x.Items.Select(i => $"{i.Name} ×{i.Quantity:0.##}")), null), (x.Channel, null),
                  (x.PaymentMethod ?? "—", null), (Math.Round(RefundAmount(x)), null));

        var amount = refunds.Sum(RefundAmount);
        var sales  = s.InPeriod.Sum(x => x.Total);
        var rate   = sales > 0 ? amount / sales : 0;
        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Refunds", Subtitle = c.Range, Category = CatMoney, ReportId = "REFUNDS",
            Kpis =
            {
                Kpi("Refunded", Math.Round(amount), "money", tone: amount > 0 ? "red" : "neutral", sub: $"{refunds.Count} refund{(refunds.Count == 1 ? "" : "s")}"),
                Kpi("Items returned", refunds.Sum(x => x.Items.Sum(i => i.Quantity))),
                Kpi("Average refund", refunds.Count > 0 ? Math.Round(amount / refunds.Count) : 0, "money"),
                Kpi("Refund rate", rate, "percent", tone: rate < 0.01 ? "green" : "amber", sub: "of sales"),
            },
            Columns =
            {
                Col("Invoice", mono: true, bold: true), Col("Date", "date"), Col("Customer"), Col("Items"),
                Col("Channel", muted: true), Col("Paid back by"), Col("Amount", "money", bold: true),
            },
            Note = "A refund is a sale marked Refunded (showroom) or Refunded / Returned (online). Installation and delivery charges are not refunded.",
        });
        dto.Totals = new List<object?> { null, null, null, null, null, null, Math.Round(amount) };
        return dto;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Customers
    // ═════════════════════════════════════════════════════════════════════════

    private AnalyticsReportDto TopCustomersReport(ReportContext c)
    {
        var s      = c.S;
        var period = s.InPeriod.ToList();
        var first  = FirstPurchase(s);
        var groups = period.Where(x => x.CustomerKey != null).GroupBy(x => x.CustomerKey!)
            .Select(g => new
            {
                Key    = g.Key,
                Latest = g.OrderByDescending(x => x.Date).First(),
                Count  = g.Count(),
                Spent  = g.Sum(x => x.Total),
                IsNew  = !s.OlderCustomerKeys.Contains(g.Key) && first.TryGetValue(g.Key, out var d) && d >= s.From,
            })
            .OrderByDescending(g => g.Spent).ToList();
        var top = groups.Take(10).ToList();
        var dueByKey = s.Open.Where(x => x.CustomerKey != null).GroupBy(x => x.CustomerKey!).ToDictionary(g => g.Key, g => g.Sum(x => x.Balance));

        var t = new TableBuilder();
        var rank = 0;
        foreach (var g in top)
        {
            var due = dueByKey.TryGetValue(g.Key, out var v) ? v : 0;
            t.Row((++rank, "muted"), (g.Latest.DisplayName, null), (g.IsNew ? "New" : "Repeat", g.IsNew ? "green" : "blue"),
                  (g.Latest.Location ?? "—", null), (g.Count, null), (Math.Round(g.Spent), null),
                  (due > 0 ? Math.Round(due) : null, due > 0 ? "amber" : null), (g.Latest.LocalDate, null));
        }

        var total   = period.Sum(x => x.Total);
        var topSum  = top.Sum(g => g.Spent);
        var biggest = period.OrderByDescending(x => x.Total).FirstOrDefault();
        var topDue  = top.Sum(g => dueByKey.TryGetValue(g.Key, out var v) ? v : 0);
        var dto = t.Into(new AnalyticsReportDto
        {
            Title = "Top customers", Subtitle = c.Range, Category = CatCustomers, ReportId = "TOP-CUSTOMERS",
            Kpis =
            {
                Kpi("Customers who bought", groups.Count, sub: $"{groups.Count(g => g.IsNew)} new"),
                Kpi($"Top {top.Count} share", total > 0 ? topSum / total : 0, "percent", tone: "green", sub: "of sales"),
                Kpi("Biggest order", Math.Round(biggest?.Total ?? 0), "money", sub: biggest?.DisplayName),
                Kpi($"Due from top {top.Count}", Math.Round(topDue), "money", tone: topDue > 0 ? "amber" : "neutral"),
            },
            Columns =
            {
                Col("#", "number", muted: true), Col("Customer", bold: true), Col("Type", "chip"), Col("District"),
                Col("Invoices", "number"), Col("Spent", "money", bold: true), Col("Still due", "money"), Col("Last order", "date"),
            },
            TotalsLabel = $"Top {top.Count} total",
        });
        dto.Totals = new List<object?> { null, null, null, null, top.Sum(g => g.Count), Math.Round(topSum), Math.Round(topDue), null };
        return dto;
    }

    private AnalyticsReportDto NewCustomersReport(ReportContext c)
    {
        var s      = c.S;
        var first  = FirstPurchase(s);
        bool IsNewIn(string key, DateTime from, DateTime to) =>
            !s.OlderCustomerKeys.Contains(key) && first.TryGetValue(key, out var d) && d >= from && d <= to;

        var newOnes = s.InPeriod.Where(x => x.CustomerKey != null && IsNewIn(x.CustomerKey, s.From, s.To))
            .GroupBy(x => x.CustomerKey!)
            .Select(g => (First: g.OrderBy(x => x.Date).First(), Spent: g.Sum(x => x.Total)))
            .OrderByDescending(n => n.First.Date).ToList();
        var prevCount = s.InPrevious.Where(x => x.CustomerKey != null && IsNewIn(x.CustomerKey, s.PrevFrom, s.PrevTo))
                                    .Select(x => x.CustomerKey).Distinct().Count();

        var t = new TableBuilder();
        foreach (var n in newOnes)
            t.Row((n.First.DisplayName, null), (n.First.LocalDate, null), (n.First.Channel, null), (n.First.Location ?? "—", null),
                  (n.First.Summary, null), (Math.Round(n.First.Total), null));

        var theirSales = newOnes.Sum(n => n.Spent);
        var allSales   = s.InPeriod.Sum(x => x.Total);
        var byChannel  = newOnes.GroupBy(n => n.First.Channel)
                                .Select(g => new NamedValueDto { Name = g.Key, Value = g.Count(), Count = g.Count() })
                                .OrderByDescending(x => x.Value).ToList();

        return t.Into(new AnalyticsReportDto
        {
            Title = "New customers", Subtitle = $"{c.Range} · first-time buyers", Category = CatCustomers, ReportId = "NEW-CUSTOMERS",
            Kpis =
            {
                Kpi("New customers", newOnes.Count, tone: "green", change: AnalyticsText.Change(newOnes.Count, prevCount), sub: $"vs {c.Previous}"),
                Kpi("Their sales", Math.Round(theirSales), "money", sub: allSales > 0 ? $"{theirSales / allSales:P0} of all sales" : null),
                Kpi("Avg first order", newOnes.Count > 0 ? Math.Round(newOnes.Average(n => n.First.Total)) : 0, "money"),
                byChannel.Count > 0
                    ? Kpi("Best channel", byChannel[0].Name, "text", sub: $"{byChannel[0].Count} new customers")
                    : Kpi("Best channel", "—", "text"),
            },
            BarsTitle  = "Where they bought",
            Bars       = AnalyticsText.WithShares(byChannel),
            BarsFormat = "number",
            TableTitle = newOnes.Count > 0 ? $"All {newOnes.Count}, latest first" : null,
            Columns =
            {
                Col("Customer", bold: true), Col("First order", "date"), Col("Channel"), Col("District"),
                Col("Bought", muted: true), Col("Value", "money"),
            },
            Note = "New = first purchase on record falls in this period (matched by phone number, else name). How customers found you isn't recorded yet.",
        });
    }

    private AnalyticsReportDto WarrantyEnding(ReportContext c)
    {
        var s    = c.S;
        var rows = WarrantiesEnding(s, 90);
        var t    = new TableBuilder();

        foreach (var w in rows)
        {
            var left    = (ToDhaka(w.Ends).Date - s.Today).Days;
            var serials = SplitSerials(w.Item.Serials);
            var serial  = serials.Count == 0 ? "—" : serials.Count == 1 ? serials[0] : $"{serials[0]} +{serials.Count - 1}";
            t.Row((w.Sale.DisplayName, null), (w.Item.Name, null), (serial, null),
                  (w.Sale.LocalDate.ToString("MMM yyyy", CultureInfo.InvariantCulture), null), (ToDhaka(w.Ends).Date, null),
                  (left, left <= 30 ? "red" : left <= 60 ? "amber" : null), (SuggestedAction(w.Item.Name), null));
        }

        var customers = rows.Select(w => w.Sale.CustomerKey ?? w.Sale.DisplayName).Distinct().Count();
        var soon      = rows.Count(w => (ToDhaka(w.Ends).Date - s.Today).Days <= 30);
        var callable  = rows.Where(w => !string.IsNullOrWhiteSpace(w.Sale.Phone)).Select(w => w.Sale.CustomerKey).Distinct().Count();

        return t.Into(new AnalyticsReportDto
        {
            Title = "Warranty ending soon",
            Subtitle = $"Warranties ending between {s.Today:d MMMM} and {s.Today.AddDays(90):d MMMM yyyy}",
            Category = CatCustomers, ReportId = "WARRANTY",
            Kpis =
            {
                Kpi("Ending in 90 days", rows.Count, tone: rows.Count > 0 ? "amber" : "neutral", sub: $"{customers} customer{(customers == 1 ? "" : "s")}"),
                Kpi("In the next 30 days", soon, tone: soon > 0 ? "red" : "neutral", sub: "call this week"),
                Kpi("Can call", callable, tone: "green", sub: "have a phone number"),
                Kpi("Products", rows.Select(w => w.Item.Name).Distinct().Count(), sub: "different products"),
            },
            Columns =
            {
                Col("Customer", bold: true), Col("Product"), Col("Serial", mono: true), Col("Installed", muted: true),
                Col("Warranty ends", "dateyear"), Col("Days left", "number"), Col("Suggested action", muted: true),
            },
            Note = "Call customers before the warranty ends — a service plan or battery check now is easier than a repair later. Phone numbers are in the Excel download.",
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private sealed record ReportContext(AnalyticsSnapshot S, string Range, string Previous, DateTime End)
    {
        /// <summary>"September 2026" for whole months, else the full range.</summary>
        public string RangeShort => Range.Split(" · ")[0];
    }

    /// <summary>"1 – 30 September 2026 · All showrooms &amp; online".</summary>
    private string PeriodText(AnalyticsSnapshot s)
    {
        var a = ToDhaka(s.From).Date;
        var b = ToDhaka(AnalyticsSnapshot.Min(s.To, s.Now)).Date;
        var range = a.Year != b.Year   ? $"{a:d MMM yyyy} – {b:d MMM yyyy}"
                  : a.Month != b.Month ? $"{a:d MMM} – {b:d MMM yyyy}"
                  :                      $"{a.Day} – {b:d MMMM yyyy}";
        return $"{range} · {s.Channel ?? "All showrooms & online"}";
    }

    /// <summary>"August" when the period is a calendar month, else "the period before".</summary>
    private string PreviousLabel(AnalyticsSnapshot s)
    {
        var a = ToDhaka(s.From);
        var b = ToDhaka(s.To);
        var wholeMonth = a.Day == 1 && a.Hour == 0 && b.Month == a.Month && b.AddDays(1).Day == 1;
        return wholeMonth ? a.AddMonths(-1).ToString("MMMM", CultureInfo.InvariantCulture) : "the period before";
    }

    private static double Discount(SaleRecord x) => x.AdditionalDiscount + x.LineDiscount + x.VoucherAmount;

    private static string? SkuOf(AnalyticsSnapshot s, string name) =>
        s.Counted.SelectMany(x => x.Items).FirstOrDefault(i => i.Name == name && !string.IsNullOrWhiteSpace(i.Sku))?.Sku;

    private static List<string> SplitSerials(string? serials) =>
        (serials ?? string.Empty).Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    private static bool IsSerialised(string name) =>
        AnalyticsText.PanelWattage(name) != null ||
        System.Text.RegularExpressions.Regex.IsMatch(name, @"inverter|ips|batter|lithium|controller|mppt|pwm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static string SuggestedAction(string product)
    {
        var n = product.ToLowerInvariant();
        if (n.Contains("batter") || n.Contains("lithium")) return "Check battery, offer upgrade";
        if (n.Contains("inverter") || n.Contains("ips"))   return "Offer 1-year service plan";
        if (AnalyticsText.PanelWattage(product) != null)   return "Offer cleaning & AMC contract";
        if (n.Contains("pump"))                            return "Pre-season pump check";
        return "Offer a service visit";
    }

    private List<AnalyticsReorderDto> BuildReorder(AnalyticsSnapshot s, Dictionary<int, double> velocity, int limit)
    {
        double? DaysLeft(ProductInfo p) =>
            velocity.TryGetValue(p.Id, out var v) && v > 0 ? Math.Round(Math.Max(0, p.Stock) / v, 1) : null;

        return s.Products
            .Where(p => velocity.TryGetValue(p.Id, out var v) && v > 0 && (p.Stock <= 0 || DaysLeft(p) < ReorderWithinDays))
            .Select(p => new AnalyticsReorderDto
            {
                ProductId    = p.Id,
                Name         = p.Name,
                Stock        = Math.Max(0, p.Stock),
                SoldLast30   = Math.Round(velocity[p.Id] * VelocityDays),
                DaysLeft     = DaysLeft(p),
                SuggestedQty = (int)(Math.Ceiling(Math.Max(1, velocity[p.Id] * VelocityDays - Math.Max(0, p.Stock)) / 5) * 5),
            })
            .OrderBy(r => r.DaysLeft ?? 0).Take(limit).ToList();
    }

    private static AnalyticsReportColumnDto Col(string label, string type = "text", bool bold = false, bool muted = false, bool mono = false) =>
        new() { Label = label, Type = type, Bold = bold, Muted = muted, Mono = mono };

    private static AnalyticsReportKpiDto Kpi(string label, object? value, string type = "number", string? sub = null,
        string tone = "neutral", double? change = null) =>
        new() { Label = label, Value = value, Type = type, Sub = sub, Tone = tone, Change = change };

    /// <summary>Collects rows with their per-cell tones, then fills a report.</summary>
    private sealed class TableBuilder
    {
        private readonly List<List<object?>> _rows  = new();
        private readonly List<List<string?>> _tones = new();

        public void Row(params (object? Value, string? Tone)[] cells)
        {
            _rows.Add(cells.Select(c => c.Value).ToList());
            _tones.Add(cells.Select(c => c.Tone).ToList());
        }

        /// <param name="sumFrom">When set, totals sum every money/number column from this index on.</param>
        public AnalyticsReportDto Into(AnalyticsReportDto dto, int? sumFrom = null)
        {
            dto.Rows  = _rows;
            dto.Tones = _tones.Any(r => r.Any(t => t != null)) ? _tones : null;
            if (sumFrom.HasValue && _rows.Count > 0)
                dto.Totals = dto.Columns.Select((col, i) =>
                    i >= sumFrom && col.Type is "money" or "number"
                        ? (object?)Math.Round(_rows.Sum(r => r[i] is IConvertible v and not string ? Convert.ToDouble(v) : 0), 2)
                        : null).ToList();
            return dto;
        }
    }
}
