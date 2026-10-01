using System;
using System.Collections.Generic;

namespace DymoEnergy.Analytics;

// ═══════════════════════════════════════════════════════════════════════════
//  Shared
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Period + channel filter shared by every analytics endpoint.</summary>
public class AnalyticsFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo   { get; set; }
    /// <summary>Invoice channel ("Showroom POS", …) or "Online shop". Null = all.</summary>
    public string?   Channel  { get; set; }
}

/// <summary>A labelled amount for bar lists (channel, category, method, city…).</summary>
public class NamedValueDto
{
    public string  Name   { get; set; } = string.Empty;
    public double  Value  { get; set; }
    /// <summary>0–1 share of the list total.</summary>
    public double  Share  { get; set; }
    /// <summary>% change vs the previous period; null when there is nothing to compare.</summary>
    public double? Change { get; set; }
    public int?    Count  { get; set; }
}

/// <summary>An actionable nudge (overview alerts, customer follow-ups).</summary>
public class AnalyticsAlertDto
{
    /// <summary>red · amber · blue · green</summary>
    public string  Tone   { get; set; } = "blue";
    public string  Title  { get; set; } = string.Empty;
    public string  Text   { get; set; } = string.Empty;
    public string? Action { get; set; }
    /// <summary>Admin route the action opens, e.g. "/products".</summary>
    public string? Link   { get; set; }
    public int?    Count  { get; set; }
    /// <summary>When set, the action opens this report instead of a route.</summary>
    public string? ReportKey { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Overview
// ═══════════════════════════════════════════════════════════════════════════

public class AnalyticsOverviewDto
{
    public double  Sales         { get; set; }
    public double  PreviousSales { get; set; }
    public double? SalesChange   { get; set; }
    /// <summary>Monthly target from settings (0 = not set).</summary>
    public double  MonthlyTarget { get; set; }

    public List<AnalyticsWeekDto>       Weeks       { get; set; } = new();
    public double                       TodaySales  { get; set; }
    public int                          TodayOrders { get; set; }
    public List<AnalyticsLatestSaleDto> LatestSales { get; set; } = new();

    public int     Orders        { get; set; }
    public double? OrdersChange  { get; set; }
    public double  AverageOrder  { get; set; }

    public int     QuoteRequests   { get; set; }
    public int     QuotesWon       { get; set; }
    public double  QuoteWinRate    { get; set; }
    public double? QuoteWinChange  { get; set; }

    public double  MoneyStillDue   { get; set; }
    public int     OverdueInvoices { get; set; }
    public double  OverdueAmount   { get; set; }

    public double  SolarKw        { get; set; }
    public double? SolarKwChange  { get; set; }
    public int     PanelsSold     { get; set; }

    public List<AnalyticsAlertDto> Alerts      { get; set; } = new();
    public List<NamedValueDto>     TopProducts { get; set; } = new();
    public List<NamedValueDto>     Channels    { get; set; } = new();
}

public class AnalyticsWeekDto
{
    public string  Label  { get; set; } = string.Empty;
    /// <summary>First and last Bangladesh-local day of the chunk.</summary>
    public DateTime From  { get; set; }
    public DateTime To    { get; set; }
    public double  Sales  { get; set; }
    public int     Orders { get; set; }
    public double? Change { get; set; }
    public bool    IsBest { get; set; }
    /// <summary>Every day in the chunk (quiet days included) for the drill-down chart.</summary>
    public List<AnalyticsDailyDto> Days { get; set; } = new();
}

public class AnalyticsLatestSaleDto
{
    public string   Channel  { get; set; } = string.Empty;
    public string   Summary  { get; set; } = string.Empty;
    public double   Total    { get; set; }
    public DateTime Date     { get; set; }
    public int?     InvoiceId { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Sales
// ═══════════════════════════════════════════════════════════════════════════

public class AnalyticsSalesDto
{
    public double  Sales          { get; set; }
    public double? SalesChange    { get; set; }
    public int     Orders         { get; set; }
    public double? OrdersChange   { get; set; }
    public double  OrdersPerDay   { get; set; }
    public double  AverageOrder   { get; set; }
    public double? AverageChange  { get; set; }
    public int     QuoteRequests  { get; set; }
    public int     QuotesWon      { get; set; }
    public double  QuoteWinRate   { get; set; }
    public double? QuoteWinChange { get; set; }

    public List<AnalyticsDailyDto>       Daily       { get; set; } = new();
    public List<NamedValueDto>           ByChannel   { get; set; } = new();
    public List<NamedValueDto>           ByCategory  { get; set; } = new();
    public AnalyticsHeatmapDto           Heatmap     { get; set; } = new();
    public List<AnalyticsFunnelStepDto>  Funnel      { get; set; } = new();
    public List<AnalyticsSalespersonDto> Salespeople { get; set; } = new();
    public List<NamedValueDto>           Discounts   { get; set; } = new();
    public double                        DiscountTotal { get; set; }
}

public class AnalyticsDailyDto
{
    public DateTime Date   { get; set; }
    public double   Sales  { get; set; }
    public int      Orders { get; set; }
}

/// <summary>Sale counts by weekday (rows, Sat→Fri) × hour (columns).</summary>
public class AnalyticsHeatmapDto
{
    public List<string> Days  { get; set; } = new();
    public List<int>    Hours { get; set; } = new();
    public List<List<int>> Cells { get; set; } = new();
    /// <summary>e.g. "4–6 pm" — the busiest 2-hour window.</summary>
    public string? PeakWindow { get; set; }
}

public class AnalyticsFunnelStepDto
{
    public string Name  { get; set; } = string.Empty;
    public int    Count { get; set; }
    /// <summary>Conversion from the previous step (0–1); null for the first.</summary>
    public double? Rate { get; set; }
}

public class AnalyticsSalespersonDto
{
    public string Name    { get; set; } = string.Empty;
    public int    Orders  { get; set; }
    public double Sales   { get; set; }
    public double Average { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Products & stock
// ═══════════════════════════════════════════════════════════════════════════

public class AnalyticsProductsDto
{
    /// <summary>Stock × selling price (cost price is not recorded).</summary>
    public double  StockValue       { get; set; }
    public int     ProductsInStock  { get; set; }
    public int     UnitsInStock     { get; set; }
    public double? DaysOfStockLeft  { get; set; }
    public int     OutOfStock       { get; set; }
    public int     RunningLow       { get; set; }

    public List<AnalyticsProductRowDto> BestSellers     { get; set; } = new();
    public List<AnalyticsReorderDto>    Reorder         { get; set; } = new();
    public List<AnalyticsSlowStockDto>  SlowStock       { get; set; } = new();
    public List<NamedValueDto>          StockByCategory { get; set; } = new();
}

public class AnalyticsProductRowDto
{
    public int?    ProductId { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  Category  { get; set; } = string.Empty;
    public double  Sold      { get; set; }
    public double  Sales     { get; set; }
    public double  Share     { get; set; }
    public int?    StockLeft { get; set; }
    public double? DaysLeft  { get; set; }
    /// <summary>ok · low · out · none (not a stocked product, e.g. a service line)</summary>
    public string  StockState { get; set; } = "ok";
}

public class AnalyticsReorderDto
{
    public int     ProductId    { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public int     Stock        { get; set; }
    public double  SoldLast30   { get; set; }
    public double? DaysLeft     { get; set; }
    public int     SuggestedQty { get; set; }
}

public class AnalyticsSlowStockDto
{
    public int       ProductId { get; set; }
    public string    Name      { get; set; } = string.Empty;
    public int       Stock     { get; set; }
    public double    Value     { get; set; }
    public DateTime? LastSold  { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Money
// ═══════════════════════════════════════════════════════════════════════════

public class AnalyticsMoneyDto
{
    public double  Collected        { get; set; }
    public double? CollectedChange  { get; set; }
    public double  StillDue         { get; set; }
    public int     DueInvoices      { get; set; }
    public int     DueCustomers     { get; set; }
    public double  Overdue          { get; set; }
    public int     OverdueInvoices  { get; set; }
    public double  Refunds          { get; set; }
    public int     RefundCount      { get; set; }
    public double? RefundsChange    { get; set; }

    public List<NamedValueDto>          ByMethod     { get; set; } = new();
    public double                       DigitalShare { get; set; }
    public List<AnalyticsAgingDto>      Aging        { get; set; } = new();
    public List<AnalyticsDebtorDto>     Debtors      { get; set; } = new();
    public List<NamedValueDto>          Expected     { get; set; } = new();
    public List<AnalyticsReminderDto>   LateInvoices { get; set; } = new();
    public double                       VatInSales   { get; set; }
    public double                       VatOnRefunds { get; set; }
}

public class AnalyticsAgingDto
{
    public string Name     { get; set; } = string.Empty;
    public double Amount   { get; set; }
    public int    Invoices { get; set; }
    /// <summary>green · amber · red</summary>
    public string Tone     { get; set; } = "green";
}

public class AnalyticsDebtorDto
{
    public string  Name     { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public int     Invoices { get; set; }
    public double  Due      { get; set; }
    /// <summary>Days past the oldest due date; null when nothing is late yet.</summary>
    public int?    LateDays { get; set; }
    /// <summary>Shown instead of late days, e.g. "not due", "COD".</summary>
    public string? Note     { get; set; }
}

public class AnalyticsReminderDto
{
    public int      InvoiceId     { get; set; }
    public string   InvoiceNumber { get; set; } = string.Empty;
    public string   Name          { get; set; } = string.Empty;
    public string?  Phone         { get; set; }
    public double   Due           { get; set; }
    public int      LateDays      { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Customers
// ═══════════════════════════════════════════════════════════════════════════

public class AnalyticsCustomersDto
{
    public int     Buyers          { get; set; }
    public double? BuyersChange    { get; set; }
    public int     NewCustomers    { get; set; }
    public double? NewChange       { get; set; }
    public int     Returning       { get; set; }
    public double  ReturningRate   { get; set; }
    public int     WarrantyEndingSoon { get; set; }

    public List<NamedValueDto>          Locations    { get; set; } = new();
    public List<AnalyticsTopCustomerDto> TopCustomers { get; set; } = new();
    public List<AnalyticsAlertDto>      FollowUps    { get; set; } = new();
}

public class AnalyticsTopCustomerDto
{
    public string  Name     { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Phone    { get; set; }
    public int     Orders   { get; set; }
    public double  Spent    { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Reports
// ═══════════════════════════════════════════════════════════════════════════

public class AnalyticsReportInputDto : AnalyticsFilterDto
{
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// A report the admin renders on screen, as a branded PDF and as CSV for Excel.
/// Mirrors the "DymoEnergy report templates" layout: category chip, title, report ID,
/// four KPI tiles, optional bar section / funnel steps / fact boxes, the table and a note.
/// </summary>
public class AnalyticsReportDto
{
    public string Key      { get; set; } = string.Empty;
    public string Title    { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    /// <summary>"Sales", "Products & stock", "Money" or "Customers" — the chip above the title.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>e.g. SALES-DAY-2609 (prefix + year/month of the period end).</summary>
    public string ReportId { get; set; } = string.Empty;

    public List<AnalyticsReportKpiDto> Kpis { get; set; } = new();
    /// <summary>Four big step tiles (quotes funnel); the last one is highlighted.</summary>
    public List<AnalyticsReportKpiDto>? Steps { get; set; }
    /// <summary>Optional horizontal bar section drawn before (or instead of) extra detail.</summary>
    public string? BarsTitle { get; set; }
    public List<NamedValueDto>? Bars { get; set; }
    /// <summary>Format for bar values: money · number.</summary>
    public string BarsFormat { get; set; } = "money";
    /// <summary>Small caption above the table, e.g. "BY DAY (2-DAY GROUPS)".</summary>
    public string? TableTitle { get; set; }

    public List<AnalyticsReportColumnDto> Columns { get; set; } = new();
    /// <summary>Raw cell values (string / number / date) — formatting is the client's job.</summary>
    public List<List<object?>> Rows { get; set; } = new();
    /// <summary>Optional per-cell tone (green · amber · red · blue · purple · muted), same shape as Rows.</summary>
    public List<List<string?>>? Tones { get; set; }
    /// <summary>Optional footer row aligned with Columns.</summary>
    public List<object?>? Totals { get; set; }
    /// <summary>Label of the footer row ("Total", "Net", "Top 10 total").</summary>
    public string TotalsLabel { get; set; } = "Total";

    /// <summary>Grey fact boxes under the table (VAT: invoices range, business details).</summary>
    public List<AnalyticsReportFactDto>? Facts { get; set; }
    public string? Note     { get; set; }
    /// <summary>neutral · amber</summary>
    public string  NoteTone { get; set; } = "neutral";
}

public class AnalyticsReportColumnDto
{
    public string Label { get; set; } = string.Empty;
    /// <summary>text · money · number · percent · date · month · chip · share (percent + mini bar)</summary>
    public string Type  { get; set; } = "text";
    /// <summary>Bold cell text (the key column, the main amount).</summary>
    public bool   Bold  { get; set; }
    /// <summary>Grey secondary text.</summary>
    public bool   Muted { get; set; }
    /// <summary>Monospace (invoice numbers, serials, SKUs).</summary>
    public bool   Mono  { get; set; }
}

public class AnalyticsReportKpiDto
{
    public string  Label { get; set; } = string.Empty;
    public object? Value { get; set; }
    /// <summary>money · number · percent · text · date</summary>
    public string  Type  { get; set; } = "number";
    public string? Sub   { get; set; }
    /// <summary>% change shown as "▲ 14% vs August" in the sub line.</summary>
    public double? Change { get; set; }
    /// <summary>neutral · green · amber · red · dark (filled)</summary>
    public string  Tone  { get; set; } = "neutral";
}

public class AnalyticsReportFactDto
{
    public string Title { get; set; } = string.Empty;
    public string Text  { get; set; } = string.Empty;
}

public class SetSalesTargetDto
{
    public double MonthlyTarget { get; set; }
}
