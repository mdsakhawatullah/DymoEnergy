using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Finance;

// ── Settings ──────────────────────────────────────────────────────────────

public class FinanceLabelDto
{
    public string Key     { get; set; } = string.Empty;
    public string Group   { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Value   { get; set; } = string.Empty;
}

public class FinanceSettingDto
{
    public string AccentColor    { get; set; } = "#0E6B3F";
    public string CurrencySymbol { get; set; } = "৳";
    public bool   CompactMoney   { get; set; } = true;
    public int    DueSoonDays    { get; set; } = 7;
    public int    AgingStep1     { get; set; } = 30;
    public int    AgingStep2     { get; set; } = 60;
    public int    ChartMonths    { get; set; } = 6;
    public string? ReminderTemplate { get; set; }
    public List<FinanceLabelDto> Labels { get; set; } = new();
}

public class UpdateFinanceSettingDto
{
    [Required, MaxLength(16)] public string AccentColor    { get; set; } = "#0E6B3F";
    [Required, MaxLength(8)]  public string CurrencySymbol { get; set; } = "৳";
    public bool CompactMoney { get; set; } = true;
    [Range(1, 60)]  public int DueSoonDays { get; set; } = 7;
    [Range(1, 365)] public int AgingStep1  { get; set; } = 30;
    [Range(2, 730)] public int AgingStep2  { get; set; } = 60;
    [Range(3, 24)]  public int ChartMonths { get; set; } = 6;
    [MaxLength(2000)] public string? ReminderTemplate { get; set; }
    public Dictionary<string, string> Labels { get; set; } = new();
}

// ── Periods ───────────────────────────────────────────────────────────────

public class FinancePeriodInput
{
    /// <summary>this-month | last-month | this-quarter | this-year</summary>
    public string? Period { get; set; }
}

public class FinancePeriodDto
{
    public string   Key       { get; set; } = "this-month";
    public string   Label     { get; set; } = string.Empty;
    public string   PrevLabel { get; set; } = string.Empty;
    public DateTime From      { get; set; }
    /// <summary>Last day of the period (inclusive).</summary>
    public DateTime To        { get; set; }
}

public class FinancePeriodOptionDto
{
    public string Key   { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

// ── Lookups & CRUD dtos ───────────────────────────────────────────────────

public class FinanceAccountDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public FinanceAccountKind Kind { get; set; }
    public string  ShortCode   { get; set; } = string.Empty;
    public string  Color       { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateTime OpeningDate   { get; set; }
    public List<string> PaymentMethods { get; set; } = new();
    public DateTime? LastReconciledOn  { get; set; }
    public bool    IsActive    { get; set; }
    public int     Order       { get; set; }

    public decimal Balance     { get; set; }
    public string  StatusText  { get; set; } = string.Empty;
    /// <summary>green | amber</summary>
    public string  StatusTone  { get; set; } = "green";
}

public class CreateUpdateFinanceAccountDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    public FinanceAccountKind Kind { get; set; } = FinanceAccountKind.Bank;
    [Required, MaxLength(8)]   public string ShortCode { get; set; } = string.Empty;
    [Required, MaxLength(16)]  public string Color { get; set; } = "#0E6B3F";
    public decimal  OpeningBalance { get; set; }
    public DateTime OpeningDate    { get; set; }
    /// <summary>SalesInvoicePaymentMethod names: Cash, Card, BankTransfer, Cheque, Other, BKash, Nagad, Rocket.</summary>
    public List<string> PaymentMethods { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int  Order    { get; set; }
}

public class FinanceCategoryDto
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  Color     { get; set; } = string.Empty;
    public FinanceCostGroup CostGroup { get; set; }
    public string  PlLine    { get; set; } = string.Empty;
    public int     Order     { get; set; }
    public bool    IsActive  { get; set; }
}

public class CreateUpdateFinanceCategoryDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(16)]  public string Color { get; set; } = "#6B7280";
    public FinanceCostGroup CostGroup { get; set; } = FinanceCostGroup.Running;
    [MaxLength(256)] public string? PlLine { get; set; }
    public int  Order    { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FinanceRecurringDto
{
    public int     Id         { get; set; }
    public string  Name       { get; set; } = string.Empty;
    public string? Detail     { get; set; }
    public int     DayOfMonth { get; set; }
    public decimal Amount     { get; set; }
    public int     CategoryId { get; set; }
    public int?    AccountId  { get; set; }
    public bool    IsActive   { get; set; }
    public int     Order      { get; set; }
    /// <summary>paid | due | scheduled — for the current month.</summary>
    public string  Status     { get; set; } = "scheduled";
}

public class CreateUpdateFinanceRecurringDto
{
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [MaxLength(256)] public string? Detail { get; set; }
    [Range(1, 31)] public int DayOfMonth { get; set; } = 1;
    [Range(0, 1000000000)] public decimal Amount { get; set; }
    public int  CategoryId { get; set; }
    public int? AccountId  { get; set; }
    public bool IsActive   { get; set; } = true;
    public int  Order      { get; set; }
}

public class PayRecurringDto
{
    public DateTime? Date      { get; set; }
    public int?      AccountId { get; set; }
    /// <summary>Override the usual amount for this month.</summary>
    public decimal?  Amount    { get; set; }
}

public class FinanceItemDto
{
    public int     Id      { get; set; }
    public FinanceItemKind Kind { get; set; }
    public string  Title   { get; set; } = string.Empty;
    public string? Detail  { get; set; }
    public string? Extra   { get; set; }
    public string? Color   { get; set; }
    public decimal Amount  { get; set; }
    public DateTime? Date  { get; set; }
    public int?    Percent { get; set; }
    public bool    Flag    { get; set; }
    public int     Order   { get; set; }
}

public class CreateUpdateFinanceItemDto
{
    public FinanceItemKind Kind { get; set; }
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Detail { get; set; }
    [MaxLength(256)]  public string? Extra  { get; set; }
    [MaxLength(16)]   public string? Color  { get; set; }
    [Range(0, 1000000000)] public decimal Amount { get; set; }
    public DateTime? Date { get; set; }
    [Range(0, 100)] public int? Percent { get; set; }
    public bool Flag  { get; set; }
    public int  Order { get; set; }
}

public class ReceiveInTransitDto
{
    public int       AccountId { get; set; }
    public DateTime? Date      { get; set; }
}

// ── Expenses ──────────────────────────────────────────────────────────────

public class FinanceExpenseDto
{
    public int     Id          { get; set; }
    public DateTime Date       { get; set; }
    public int     CategoryId  { get; set; }
    public string  CategoryName  { get; set; } = string.Empty;
    public string  CategoryColor { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
    public decimal Amount      { get; set; }
    public int?    AccountId   { get; set; }
    public string  PaidBy      { get; set; } = string.Empty;
    public string? PaidByNote  { get; set; }
    public string? ReceiptUrl  { get; set; }
    public int?    RecurringCostId { get; set; }
}

public class CreateUpdateFinanceExpenseDto
{
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    [Required, MaxLength(256)] public string Description { get; set; } = string.Empty;
    [Range(0.01, 1000000000)] public decimal Amount { get; set; }
    /// <summary>Account the money left from. Leave empty if it was paid some other way.</summary>
    public int? AccountId { get; set; }
    [MaxLength(256)]  public string? PaidByNote { get; set; }
    [MaxLength(1024)] public string? ReceiptUrl { get; set; }
}

// ── Supplier bills ────────────────────────────────────────────────────────

public class FinanceBillPaymentDto
{
    public int      Id        { get; set; }
    public DateTime Date      { get; set; }
    public decimal  Amount    { get; set; }
    public int      AccountId { get; set; }
    public string   AccountName { get; set; } = string.Empty;
    public string?  Reference { get; set; }
}

public class FinanceBillDto
{
    public int     Id          { get; set; }
    public string  Supplier    { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BillNumber  { get; set; }
    public DateTime BillDate   { get; set; }
    public DateTime DueDate    { get; set; }
    public decimal Amount      { get; set; }
    public int     CategoryId  { get; set; }
    public string  CategoryName { get; set; } = string.Empty;
    public string? ReceiptUrl  { get; set; }
    public string? Note        { get; set; }

    public decimal Paid        { get; set; }
    public decimal Remaining   { get; set; }
    /// <summary>paid | part | open | overdue</summary>
    public string  Status      { get; set; } = "open";
    public int     DueInDays   { get; set; }
    public DateTime? LastPaidOn { get; set; }
    public List<FinanceBillPaymentDto> Payments { get; set; } = new();
}

public class CreateUpdateFinanceBillDto
{
    [Required, MaxLength(256)] public string Supplier { get; set; } = string.Empty;
    [MaxLength(256)] public string? Description { get; set; }
    [MaxLength(128)] public string? BillNumber  { get; set; }
    public DateTime BillDate { get; set; }
    public DateTime DueDate  { get; set; }
    [Range(0.01, 1000000000)] public decimal Amount { get; set; }
    public int CategoryId { get; set; }
    [MaxLength(1024)] public string? ReceiptUrl { get; set; }
    [MaxLength(2000)] public string? Note { get; set; }
}

public class PayFinanceBillDto
{
    [Range(0.01, 1000000000)] public decimal Amount { get; set; }
    public int      AccountId { get; set; }
    public DateTime? Date     { get; set; }
    [MaxLength(256)] public string? Reference { get; set; }
}

// ── Ledger rows ───────────────────────────────────────────────────────────

public class CreateFinanceTransactionDto
{
    public FinanceDirection Direction { get; set; }
    public int      AccountId { get; set; }
    [Range(0.01, 1000000000)] public decimal Amount { get; set; }
    public DateTime? Date { get; set; }
    [MaxLength(256)]  public string? Category    { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(256)]  public string? Reference   { get; set; }
}

public class CreateFinanceTransferDto
{
    public int FromAccountId { get; set; }
    public int ToAccountId   { get; set; }
    [Range(0.01, 1000000000)] public decimal Amount { get; set; }
    public DateTime? Date { get; set; }
    [MaxLength(2000)] public string? Note { get; set; }
}

public class FinanceMovementDto
{
    /// <summary>tx = a ledger row; invoice = a customer payment read from the sales invoice.</summary>
    public string  Source    { get; set; } = "tx";
    public int     RefId     { get; set; }
    public DateTime Date     { get; set; }
    public FinanceDirection Direction { get; set; }
    public string  Title     { get; set; } = string.Empty;
    public string? Category  { get; set; }
    public int?    AccountId { get; set; }
    public string  AccountName { get; set; } = string.Empty;
    public decimal Amount    { get; set; }
    public decimal? BalanceAfter { get; set; }
    public bool    IsTransfer { get; set; }
}

// ── Page: overview ────────────────────────────────────────────────────────

public class FinanceUnassignedDto
{
    public int     Count  { get; set; }
    public decimal Amount { get; set; }
    public List<string> Methods { get; set; } = new();
}

public class FinanceOverviewDto
{
    public FinanceSettingDto Setting { get; set; } = new();
    public FinancePeriodDto  Period  { get; set; } = new();
    public List<FinancePeriodOptionDto> Periods { get; set; } = new();

    public decimal MoneyToUse    { get; set; }
    public int     AccountCount  { get; set; }
    public decimal OnTheWayTotal { get; set; }
    public int     OnTheWayCount { get; set; }
    public int?    OnTheWayLatestDays { get; set; }
    public decimal CustomersOwe  { get; set; }
    public int     CustomersOweCount { get; set; }
    public int     LateInvoices  { get; set; }
    public decimal SuppliersOwe  { get; set; }
    public int     OpenBills     { get; set; }
    public int     OverdueBills  { get; set; }
    public decimal Profit        { get; set; }
    public string  ProfitPeriodLabel { get; set; } = string.Empty;

    public FinanceUnassignedDto Unassigned { get; set; } = new();
    public List<FinanceAccountDto>  Accounts   { get; set; } = new();
    public List<FinanceCategoryDto> Categories { get; set; } = new();

    // Everything the "Customise" drawer edits, including switched-off entries.
    public List<FinanceAccountDto>   AllAccounts   { get; set; } = new();
    public List<FinanceCategoryDto>  AllCategories { get; set; } = new();
    public List<FinanceRecurringDto> Recurring     { get; set; } = new();
    public List<FinanceItemDto>      Items         { get; set; } = new();
}

// ── Page: cash ────────────────────────────────────────────────────────────

public class FinanceFlowLineDto
{
    public string  Label  { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FinanceCashDto
{
    public List<FinanceAccountDto> Accounts { get; set; } = new();
    public List<FinanceItemDto>    OnTheWay { get; set; } = new();
    public decimal MoneyIn  { get; set; }
    public decimal MoneyOut { get; set; }
    public List<FinanceFlowLineDto> InLines  { get; set; } = new();
    public List<FinanceFlowLineDto> OutLines { get; set; } = new();
    public List<FinanceMovementDto> Movements { get; set; } = new();
}

// ── Page: customer dues ───────────────────────────────────────────────────

public class FinanceDueDto
{
    public int     InvoiceId     { get; set; }
    public string  InvoiceNumber { get; set; } = string.Empty;
    public string  CustomerName  { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? Channel       { get; set; }
    public DateTime InvoiceDate  { get; set; }
    public DateTime? DueDate     { get; set; }
    public decimal Balance       { get; set; }
    /// <summary>Whole days past the due date; null when not late.</summary>
    public int?    DaysLate      { get; set; }
}

public class FinanceAgingDto
{
    public string  Key    { get; set; } = string.Empty;
    public string  Label  { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int     Count  { get; set; }
}

public class FinanceDealerDto
{
    public int     Id     { get; set; }
    public string  Name   { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public decimal Limit  { get; set; }
    public decimal Used   { get; set; }
    public decimal Left   { get; set; }
    public List<FinanceDueDto> OpenInvoices { get; set; } = new();
}

public class FinanceDuesDto
{
    public decimal Total       { get; set; }
    public int     Count       { get; set; }
    public decimal LateAmount  { get; set; }
    public int     LateCount   { get; set; }
    public decimal Collected   { get; set; }
    public decimal? CollectedPercentOfSales { get; set; }
    public double? AverageDaysToPay { get; set; }
    public double? PreviousAverageDaysToPay { get; set; }
    public List<FinanceAgingDto>  Aging    { get; set; } = new();
    public List<FinanceItemDto>   Advances { get; set; } = new();
    public decimal AdvancesTotal { get; set; }
    public List<FinanceDueDto>    Dues     { get; set; } = new();
    public List<FinanceDealerDto> Dealers  { get; set; } = new();
}

// ── Page: supplier bills ──────────────────────────────────────────────────

public class FinancePlanWeekDto
{
    public string  Label    { get; set; } = string.Empty;
    public decimal ToPay    { get; set; }
    public decimal Expected { get; set; }
}

public class FinanceBillsDto
{
    public decimal Owe          { get; set; }
    public int     OpenCount    { get; set; }
    public decimal DueSoonAmount { get; set; }
    public int     DueSoonCount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int     OverdueCount { get; set; }
    public int     OverdueMaxDays { get; set; }
    public decimal PaidInPeriod { get; set; }
    public int     PaidCount    { get; set; }
    public List<FinanceBillDto>     Bills { get; set; } = new();
    public List<FinancePlanWeekDto> Plan  { get; set; } = new();
    /// <summary>Label of the first week where money to pay exceeds money expected; null when none.</summary>
    public string? TightWeek    { get; set; }
    public List<FinanceItemDto> LettersOfCredit { get; set; } = new();
}

// ── Page: expenses ────────────────────────────────────────────────────────

public class FinanceCostLineDto
{
    public int     CategoryId { get; set; }
    public string  Name   { get; set; } = string.Empty;
    public string  Color  { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int     Count  { get; set; }
    /// <summary>% change against the previous period; null when there was nothing before.</summary>
    public int?    ChangePercent { get; set; }
}

public class FinanceExpensesDto
{
    public decimal Running         { get; set; }
    public decimal? RunningPercentOfSales { get; set; }
    public string? BiggestName     { get; set; }
    public decimal BiggestAmount   { get; set; }
    public int     BiggestCount    { get; set; }
    public string? UpMostName      { get; set; }
    public int?    UpMostPercent   { get; set; }
    public decimal? CostPerOrder   { get; set; }
    public int     OrderCount      { get; set; }
    public decimal TotalSpent      { get; set; }
    public List<FinanceCostLineDto>  Categories { get; set; } = new();
    public List<FinanceRecurringDto> Recurring  { get; set; } = new();
    public decimal RecurringTotal  { get; set; }
    public List<FinanceExpenseDto>   Latest     { get; set; } = new();
    public int     PeriodExpenseCount { get; set; }
}

// ── Page: profit & loss ───────────────────────────────────────────────────

public class FinancePnlLineDto
{
    /// <summary>sales | cost | running</summary>
    public string  Section  { get; set; } = string.Empty;
    public string  Label    { get; set; } = string.Empty;
    /// <summary>Costs are shown negative.</summary>
    public decimal Current  { get; set; }
    public decimal Previous { get; set; }
    public int?    ChangePercent { get; set; }
    public bool    IsTotal  { get; set; }
    public string  Key      { get; set; } = string.Empty;
}

public class FinanceBarDto
{
    public string  Label    { get; set; } = string.Empty;
    public decimal Profit   { get; set; }
    public bool    IsCurrent { get; set; }
}

public class FinanceInsightDto
{
    /// <summary>best | dip</summary>
    public string  Kind    { get; set; } = string.Empty;
    public string  Label   { get; set; } = string.Empty;
    public decimal Amount  { get; set; }
    public int?    PercentVsAverage { get; set; }
}

public class FinancePnlDto
{
    public List<FinancePnlLineDto> Lines    { get; set; } = new();
    public List<FinanceBarDto>     Bars     { get; set; } = new();
    public List<FinanceInsightDto> Insights { get; set; } = new();
    public List<FinanceItemDto>    Notes    { get; set; } = new();
}

// ── Export ────────────────────────────────────────────────────────────────

public class FinanceExportDto
{
    public string FileName { get; set; } = string.Empty;
}
