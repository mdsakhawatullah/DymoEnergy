using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.Finance;

/// <summary>Single-row settings. Money amounts everywhere in the Finance module are <c>decimal</c>.</summary>
public class FinanceSetting : AuditedAggregateRoot<int>
{
    public string  AccentColor      { get; set; } = "#0E6B3F";
    public string  CurrencySymbol   { get; set; } = "৳";
    /// <summary>Show 4,70,000 as ৳4.7L on cards (Bangladesh lakh style).</summary>
    public bool    CompactMoney     { get; set; } = true;
    public int     DueSoonDays      { get; set; } = 7;
    /// <summary>Upper bound (days late) of the first and second ageing buckets.</summary>
    public int     AgingStep1       { get; set; } = 30;
    public int     AgingStep2       { get; set; } = 60;
    public int     ChartMonths      { get; set; } = 6;
    /// <summary>Message copied for "Remind"; {name} {invoice} {amount} {days} are replaced.</summary>
    public string? ReminderTemplate { get; set; }
    public string? LabelsJson       { get; set; }
}

public class FinanceAccount : FullAuditedAggregateRoot<int>
{
    public string  Name           { get; set; } = string.Empty;
    public FinanceAccountKind Kind { get; set; } = FinanceAccountKind.Bank;
    public string  ShortCode      { get; set; } = string.Empty;
    public string  Color          { get; set; } = "#0E6B3F";
    public decimal OpeningBalance { get; set; }
    /// <summary>Movements before this day are already inside the opening balance.</summary>
    public DateTime OpeningDate   { get; set; }
    /// <summary>Comma separated <c>SalesInvoicePaymentMethod</c> names whose customer payments land here.</summary>
    public string? PaymentMethods { get; set; }
    public DateTime? LastReconciledOn { get; set; }
    public bool    IsActive       { get; set; } = true;
    public int     Order          { get; set; }
}

/// <summary>
/// A manual ledger row. Customer payments are NOT stored here — they are read from the invoice
/// payments so money is never counted twice.
/// </summary>
public class FinanceTransaction : FullAuditedAggregateRoot<int>
{
    public DateTime Date       { get; set; }
    public int      AccountId  { get; set; }
    public FinanceDirection Direction { get; set; }
    public decimal  Amount     { get; set; }
    public string?  Category   { get; set; }
    public string?  Description { get; set; }
    public string?  Reference  { get; set; }
    public FinanceTxSource Source { get; set; }
    /// <summary>Bill / expense / in-transit id, depending on <see cref="Source"/>; transfers share a pair id here.</summary>
    public int?     SourceId   { get; set; }
}

public class FinanceExpenseCategory : FullAuditedAggregateRoot<int>
{
    public string  Name      { get; set; } = string.Empty;
    public string  Color     { get; set; } = "#6B7280";
    public FinanceCostGroup CostGroup { get; set; } = FinanceCostGroup.Running;
    /// <summary>Label of the profit-and-loss line this category rolls into (several categories can share one).</summary>
    public string  PlLine    { get; set; } = string.Empty;
    public int     Order     { get; set; }
    public bool    IsActive  { get; set; } = true;
}

public class FinanceExpense : FullAuditedAggregateRoot<int>
{
    public DateTime Date        { get; set; }
    public int      CategoryId  { get; set; }
    public string   Description { get; set; } = string.Empty;
    public decimal  Amount      { get; set; }
    /// <summary>When set, a matching money-out row exists in the ledger.</summary>
    public int?     AccountId   { get; set; }
    public string?  PaidByNote  { get; set; }
    public string?  ReceiptUrl  { get; set; }
    public int?     RecurringCostId { get; set; }
}

public class FinanceSupplierBill : FullAuditedAggregateRoot<int>
{
    public string   Supplier    { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public string?  BillNumber  { get; set; }
    public DateTime BillDate    { get; set; }
    public DateTime DueDate     { get; set; }
    public decimal  Amount      { get; set; }
    public int      CategoryId  { get; set; }
    public string?  ReceiptUrl  { get; set; }
    public string?  Note        { get; set; }
}

public class FinanceRecurringCost : FullAuditedAggregateRoot<int>
{
    public string  Name        { get; set; } = string.Empty;
    public string? Detail      { get; set; }
    public int     DayOfMonth  { get; set; } = 1;
    public decimal Amount      { get; set; }
    public int     CategoryId  { get; set; }
    /// <summary>Account normally used to pay it; pre-selected when you record the payment.</summary>
    public int?    AccountId   { get; set; }
    public bool    IsActive    { get; set; } = true;
    public int     Order       { get; set; }
}

/// <summary>Generic list entry; <see cref="Kind"/> decides what the other fields mean.</summary>
public class FinanceListItem : FullAuditedAggregateRoot<int>
{
    public FinanceItemKind Kind { get; set; }
    public string   Title   { get; set; } = string.Empty;
    public string?  Detail  { get; set; }
    public string?  Extra   { get; set; }
    public string?  Color   { get; set; }
    public decimal  Amount  { get; set; }
    public DateTime? Date   { get; set; }
    public int?     Percent { get; set; }
    public bool     Flag    { get; set; }
    public int      Order   { get; set; }
}
