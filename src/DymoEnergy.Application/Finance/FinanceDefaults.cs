using System.Collections.Generic;

namespace DymoEnergy.Finance;

/// <summary>Built-in wording and starter lists; everything here can be edited from the page.</summary>
public static class FinanceDefaults
{
    public record LabelDef(string Key, string Group, string Caption, string Default);

    public const string ReminderTemplate =
        "Dear {name}, invoice {invoice} for {amount} is {days} overdue. Please arrange payment. Thank you.";

    public static readonly IReadOnlyList<LabelDef> Labels = new List<LabelDef>
    {
        new("page.title",    "Page", "Page title", "Financials & billing"),
        new("page.subtitle", "Page", "Subtitle", "Where the money is, who owes you, what you owe, and what is left at the end of the month."),
        new("btn.record",    "Page", "Record button", "Record payment"),
        new("btn.export",    "Page", "Export button", "Export for accountant"),

        new("kpi.cash",        "Summary cards", "Big card: heading", "Money you can use today"),
        new("kpi.cashNote",    "Summary cards", "Big card: note ({n} accounts, {amount} on the way)", "across {n} accounts · {amount} more on the way"),
        new("kpi.way",         "Summary cards", "Money on the way card", "Money on the way"),
        new("kpi.wayNote",     "Summary cards", "…note", "settles within a week"),
        new("kpi.owe",         "Summary cards", "Customers owe you card", "Customers owe you"),
        new("kpi.oweNote",     "Summary cards", "…note ({n} late invoices)", "{n} invoices are late"),
        new("kpi.oweNote.one", "Summary cards", "…note when exactly one", "{n} invoice is late"),
        new("kpi.suppliers",   "Summary cards", "You owe suppliers card", "You owe suppliers"),
        new("kpi.suppliersNote", "Summary cards", "…note ({n} overdue bills)", "{n} bills are overdue"),
        new("kpi.suppliersNote.one", "Summary cards", "…note when exactly one", "{n} bill is overdue"),
        new("kpi.profit",      "Summary cards", "Profit card ({period})", "Profit {period}"),
        new("kpi.profitNote",  "Summary cards", "…note", "after running costs"),

        new("tab.cash",     "Tabs", "Cash tab", "Cash & bank"),
        new("tab.dues",     "Tabs", "Customer dues tab", "Customer dues"),
        new("tab.bills",    "Tabs", "Supplier bills tab", "Supplier bills"),
        new("tab.expenses", "Tabs", "Expenses tab", "Expenses"),
        new("tab.pnl",      "Tabs", "Profit & loss tab", "Profit & loss"),

        new("cash.way",      "Cash & bank", "Money on the way heading", "Money on the way"),
        new("cash.waySub",   "Cash & bank", "…subtitle", "collected, not in your account yet"),
        new("cash.wayNote",  "Cash & bank", "…note", "Card and courier money is already yours — chase it if it does not arrive on time."),
        new("cash.inout",    "Cash & bank", "In and out heading", "In and out"),
        new("cash.in",       "Cash & bank", "Money in", "Money in"),
        new("cash.out",      "Cash & bank", "Money out", "Money out"),
        new("cash.kept",     "Cash & bank", "Kept", "kept"),
        new("cash.latest",   "Cash & bank", "Movements heading", "Latest money movements"),
        new("cash.latestSub","Cash & bank", "…subtitle", "all accounts"),

        new("dues.owe",       "Customer dues", "Stat: customers owe you", "Customers owe you"),
        new("dues.late",      "Customer dues", "Stat: late", "Late"),
        new("dues.collected", "Customer dues", "Stat: collected ({period})", "Collected {period}"),
        new("dues.avg",       "Customer dues", "Stat: average days to pay", "Average days to pay"),
        new("dues.aging",     "Customer dues", "Ageing heading", "How old is the money owed"),
        new("dues.remindAll", "Customer dues", "Remind-all button", "Copy reminders for everyone late"),
        new("dues.advances",  "Customer dues", "Advances heading", "Advances held"),
        new("dues.advancesSub","Customer dues", "…subtitle", "Money taken before the job is done. It is not yours until the system is handed over."),
        new("dues.table",     "Customer dues", "Table heading", "Who owes you"),
        new("dues.tableSub",  "Customer dues", "…subtitle", "largest first · late marked red"),
        new("dues.typeCol",   "Customer dues", "Column: type", "Channel"),
        new("dues.dealers",   "Customer dues", "Dealer credit heading", "Dealer credit"),
        new("dues.dealersSub","Customer dues", "…subtitle", "installers who buy on account"),

        new("bills.owe",      "Supplier bills", "Stat: you owe suppliers", "You owe suppliers"),
        new("bills.week",     "Supplier bills", "Stat: due this week", "Due this week"),
        new("bills.overdue",  "Supplier bills", "Stat: overdue", "Overdue"),
        new("bills.paid",     "Supplier bills", "Stat: paid ({period})", "Paid {period}"),
        new("bills.table",    "Supplier bills", "Table heading", "Bills to pay"),
        new("bills.tableSub", "Supplier bills", "…subtitle", "soonest first"),
        new("bills.plan",     "Supplier bills", "Payment plan heading", "Payment plan · next 30 days"),
        new("bills.planSub",  "Supplier bills", "…subtitle", "Compare what you must pay with the money you expect in."),
        new("bills.planAdvice","Supplier bills", "Advice after a tight week", "Ask your suppliers for more days, or chase customer payments first."),
        new("bills.planOk",   "Supplier bills", "Message when no week is tight", "Money coming in covers what you must pay over the next 30 days."),
        new("bills.lc",       "Supplier bills", "Letters of credit heading", "Import payments (LC)"),
        new("bills.lcSub",    "Supplier bills", "…subtitle", "Letters of credit open with the bank, tied to the shipment they pay for."),

        new("exp.running",    "Expenses", "Stat: running costs ({period})", "Running costs {period}"),
        new("exp.biggest",    "Expenses", "Stat: biggest cost", "Biggest cost"),
        new("exp.upMost",     "Expenses", "Stat: up the most", "Up the most"),
        new("exp.perOrder",   "Expenses", "Stat: cost per order", "Cost per order"),
        new("exp.orders",     "Expenses", "…orders word ({n})", "{n} orders"),
        new("exp.where",      "Expenses", "Where the money went heading", "Where the money went"),
        new("exp.monthly",    "Expenses", "Monthly costs heading", "Monthly running costs"),
        new("exp.monthlySub", "Expenses", "…subtitle", "paid every month"),
        new("exp.monthlyTotal","Expenses", "…total row", "Every month"),
        new("exp.latest",     "Expenses", "Latest expenses heading", "Latest expenses"),

        new("pl.title",       "Profit & loss", "Statement heading", "Profit & loss"),
        new("pl.vs",          "Profit & loss", "Comparison label ({prev})", "vs {prev}"),
        new("pl.secSales",    "Profit & loss", "Section: sales", "Sales"),
        new("pl.secCost",     "Profit & loss", "Section: cost of what you sold", "Cost of what you sold"),
        new("pl.secRunning",  "Profit & loss", "Section: running costs", "Running costs"),
        new("pl.sales",       "Profit & loss", "Line: sales", "Sales (VAT included)"),
        new("pl.vat",         "Profit & loss", "Line: VAT", "VAT included in sales"),
        new("pl.net",         "Profit & loss", "Line: sales before VAT", "Sales before VAT"),
        new("pl.gross",       "Profit & loss", "Line: gross profit", "Gross profit"),
        new("pl.profit",      "Profit & loss", "Line: profit", "Profit for the period"),
        new("pl.note",        "Profit & loss", "Note under the statement", "Product cost comes from supplier bills dated in the period, running costs from expenses and bills. VAT is taken out so the profit line is comparable month to month. Refunded invoices are left out."),
        new("pl.chart",       "Profit & loss", "Chart heading ({n} months)", "Profit, last {n} months"),
        new("pl.chartNote",   "Profit & loss", "Note under the chart", "After product cost and running costs."),
        new("pl.notes",       "Profit & loss", "Worth knowing heading", "Worth knowing"),
        new("pl.bestTitle",   "Profit & loss", "Best month title ({month})", "{month} was the best month"),
        new("pl.bestText",    "Profit & loss", "Best month text ({amount}, {pct})", "{amount} profit, {pct}% above the average."),
        new("pl.dipTitle",    "Profit & loss", "Weakest month title ({month})", "{month} was the weakest month"),
        new("pl.dipText",     "Profit & loss", "Weakest month text ({amount}, {pct})", "{amount} profit, {pct}% below the average."),
        new("pl.disclaimer",  "Profit & loss", "Disclaimer box", "This is a working summary for the owner, not a filed account. Your accountant's figures are the official ones."),
    };

    public record AccountSeed(string Name, FinanceAccountKind Kind, string Code, string Color, string Methods);

    public static readonly AccountSeed[] Accounts =
    {
        new("Cash in hand",     FinanceAccountKind.Cash,         "CASH", "#0E6B3F", "Cash"),
        new("bKash merchant",   FinanceAccountKind.MobileWallet, "bK",   "#DB2777", "BKash"),
        new("Nagad merchant",   FinanceAccountKind.MobileWallet, "N",    "#D97706", "Nagad"),
        new("Bank current account", FinanceAccountKind.Bank,     "BNK",  "#2563EB", "BankTransfer,Cheque,Card,Rocket,Other"),
    };

    public record CategorySeed(string Name, string Color, FinanceCostGroup Group, string PlLine);

    public static readonly CategorySeed[] Categories =
    {
        new("Product cost",       "#2563EB", FinanceCostGroup.CostOfSales, "Product cost"),
        new("Installation labour","#D97706", FinanceCostGroup.CostOfSales, "Installation labour"),
        new("Salaries",           "#7C3AED", FinanceCostGroup.Running,     "Salaries"),
        new("Rent",               "#6B7280", FinanceCostGroup.Running,     "Rent & utilities"),
        new("Utilities",          "#1E40AF", FinanceCostGroup.Running,     "Rent & utilities"),
        new("Transport",          "#2563EB", FinanceCostGroup.Running,     "Transport & marketing"),
        new("Marketing",          "#DB2777", FinanceCostGroup.Running,     "Transport & marketing"),
        new("Fuel & vehicle",     "#0F766E", FinanceCostGroup.Running,     "Other"),
        new("Office & other",     "#6B7280", FinanceCostGroup.Running,     "Other"),
    };
}
