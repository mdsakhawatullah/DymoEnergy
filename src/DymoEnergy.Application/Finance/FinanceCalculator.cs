using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.SalesInvoices;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace DymoEnergy.Finance;

/// <summary>A resolved reporting period plus the one before it (same length, for "vs last period").</summary>
public sealed record FinancePeriod(string Key, string Label, string PrevLabel, DateTime From, DateTime To, DateTime PrevFrom, DateTime PrevTo);

/// <summary>One row of money moving in or out of an account, from the ledger or from a customer payment.</summary>
public sealed class FinanceMove
{
    public string Source { get; init; } = "tx";           // tx | invoice
    public int RefId { get; init; }
    public DateTime Date { get; init; }
    public FinanceDirection Direction { get; init; }
    public decimal Amount { get; init; }
    public int? AccountId { get; set; }
    public string Title { get; init; } = string.Empty;
    public string? Category { get; init; }
    public bool IsTransfer { get; init; }
    public decimal? BalanceAfter { get; set; }
}

/// <summary>Everything the Finance page needs, loaded once per request.</summary>
public sealed class FinanceContext
{
    public FinanceSetting Setting { get; init; } = new();
    public DateTime Today { get; init; }
    public List<FinanceAccount> Accounts { get; init; } = new();
    public List<FinanceTransaction> Txs { get; init; } = new();
    public List<SalesInvoice> Invoices { get; init; } = new();
    public List<SalesInvoicePayment> Payments { get; init; } = new();
    public List<FinanceSupplierBill> Bills { get; init; } = new();
    public List<FinanceExpense> Expenses { get; init; } = new();
    public List<FinanceExpenseCategory> Categories { get; init; } = new();
    public List<FinanceRecurringCost> Recurring { get; init; } = new();
    public List<FinanceListItem> Items { get; init; } = new();

    public Dictionary<int, SalesInvoice> InvoiceById { get; init; } = new();
    public Dictionary<string, int> MethodAccount { get; init; } = new();
    public List<FinanceMove> Moves { get; init; } = new();
    public Dictionary<int, decimal> Balances { get; init; } = new();
}

public class FinanceCalculator : ITransientDependency
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private const decimal Eps = 0.005m;

    private readonly IRepository<FinanceSetting, int>         _settings;
    private readonly IRepository<FinanceAccount, int>         _accounts;
    private readonly IRepository<FinanceTransaction, int>     _txs;
    private readonly IRepository<FinanceExpenseCategory, int> _categories;
    private readonly IRepository<FinanceExpense, int>         _expenses;
    private readonly IRepository<FinanceSupplierBill, int>    _bills;
    private readonly IRepository<FinanceRecurringCost, int>   _recurring;
    private readonly IRepository<FinanceListItem, int>        _items;
    private readonly IRepository<SalesInvoice, int>           _invoices;
    private readonly IRepository<SalesInvoicePayment, int>    _payments;
    private readonly IClock _clock;

    public FinanceCalculator(
        IRepository<FinanceSetting, int> settings, IRepository<FinanceAccount, int> accounts,
        IRepository<FinanceTransaction, int> txs, IRepository<FinanceExpenseCategory, int> categories,
        IRepository<FinanceExpense, int> expenses, IRepository<FinanceSupplierBill, int> bills,
        IRepository<FinanceRecurringCost, int> recurring, IRepository<FinanceListItem, int> items,
        IRepository<SalesInvoice, int> invoices, IRepository<SalesInvoicePayment, int> payments, IClock clock)
    {
        _settings = settings; _accounts = accounts; _txs = txs; _categories = categories; _expenses = expenses;
        _bills = bills; _recurring = recurring; _items = items; _invoices = invoices; _payments = payments; _clock = clock;
    }

    // ══ PERIODS ══════════════════════════════════════════════════════════════

    public static readonly (string Key, string Label)[] PeriodOptions =
    {
        ("this-month", "This month"), ("last-month", "Last month"), ("this-quarter", "This quarter"), ("this-year", "This year"),
    };

    public static FinancePeriod ResolvePeriod(string? key, DateTime today)
    {
        today = today.Date;
        var inv = CultureInfo.InvariantCulture;
        DateTime from, to, pFrom, pTo;
        string label, prev;

        switch (key)
        {
            case "last-month":
                from = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                to = from.AddMonths(1).AddDays(-1);
                pFrom = from.AddMonths(-1); pTo = from.AddDays(-1);
                label = from.ToString("MMMM", inv); prev = pFrom.ToString("MMMM", inv);
                break;
            case "this-quarter":
                var qStartMonth = ((today.Month - 1) / 3) * 3 + 1;
                from = new DateTime(today.Year, qStartMonth, 1);
                to = from.AddMonths(3).AddDays(-1);
                pFrom = from.AddMonths(-3); pTo = from.AddDays(-1);
                label = $"Q{(qStartMonth - 1) / 3 + 1} {from.Year}"; prev = $"Q{(pFrom.Month - 1) / 3 + 1} {pFrom.Year}";
                break;
            case "this-year":
                from = new DateTime(today.Year, 1, 1); to = new DateTime(today.Year, 12, 31);
                pFrom = from.AddYears(-1); pTo = from.AddDays(-1);
                label = from.Year.ToString(inv); prev = pFrom.Year.ToString(inv);
                break;
            default:
                key = "this-month";
                from = new DateTime(today.Year, today.Month, 1);
                to = from.AddMonths(1).AddDays(-1);
                pFrom = from.AddMonths(-1); pTo = from.AddDays(-1);
                label = from.ToString("MMMM", inv); prev = pFrom.ToString("MMMM", inv);
                break;
        }
        return new FinancePeriod(key!, label, prev, from, to, pFrom, pTo);
    }

    // ══ LOADING ══════════════════════════════════════════════════════════════

    public async Task<FinanceContext> LoadAsync()
    {
        await EnsureDefaultsAsync();

        var setting  = (await _settings.GetListAsync()).OrderBy(s => s.Id).First();
        var accounts = (await _accounts.GetListAsync()).OrderBy(a => a.Order).ThenBy(a => a.Id).ToList();
        var invoices = await _invoices.GetListAsync();
        var byId     = invoices.ToDictionary(i => i.Id);
        var payments = (await _payments.GetListAsync()).Where(p => byId.ContainsKey(p.InvoiceId)).ToList();

        var ctx = new FinanceContext
        {
            Setting = setting,
            Today = _clock.Now.Date,
            Accounts = accounts,
            Txs = await _txs.GetListAsync(),
            Invoices = invoices,
            Payments = payments,
            Bills = await _bills.GetListAsync(),
            Expenses = await _expenses.GetListAsync(),
            Categories = (await _categories.GetListAsync()).OrderBy(c => c.Order).ThenBy(c => c.Id).ToList(),
            Recurring = (await _recurring.GetListAsync()).OrderBy(r => r.DayOfMonth).ThenBy(r => r.Order).ToList(),
            Items = (await _items.GetListAsync()).OrderBy(i => i.Kind).ThenBy(i => i.Order).ThenBy(i => i.Id).ToList(),
            InvoiceById = byId,
        };

        // Which account each customer payment method lands in (first account in order wins).
        foreach (var a in ctx.Accounts.Where(a => a.IsActive))
            foreach (var m in SplitCsv(a.PaymentMethods))
                ctx.MethodAccount.TryAdd(m, a.Id);

        BuildMoves(ctx);
        return ctx;
    }

    private static void BuildMoves(FinanceContext ctx)
    {
        foreach (var p in ctx.Payments)
        {
            var inv = ctx.InvoiceById[p.InvoiceId];
            ctx.Moves.Add(new FinanceMove
            {
                Source = "invoice", RefId = p.Id, Date = p.PaidOn.Date, Direction = FinanceDirection.In,
                Amount = Money(p.Amount),
                AccountId = ctx.MethodAccount.TryGetValue(p.Method.ToString(), out var acc) ? acc : null,
                Title = $"{inv.CustomerName ?? "Customer"} · {inv.InvoiceNumber}",
                Category = string.IsNullOrWhiteSpace(inv.Channel) ? "Customer payments" : inv.Channel,
            });
        }

        foreach (var t in ctx.Txs)
        {
            ctx.Moves.Add(new FinanceMove
            {
                Source = "tx", RefId = t.Id, Date = t.Date.Date, Direction = t.Direction, Amount = t.Amount, AccountId = t.AccountId,
                Title = !string.IsNullOrWhiteSpace(t.Description) ? t.Description! : t.Category ?? "Money movement",
                Category = t.Category, IsTransfer = t.Source == FinanceTxSource.Transfer,
            });
        }

        // Running balance per account, counting only what happened on/after its opening date.
        foreach (var a in ctx.Accounts)
        {
            var balance = a.OpeningBalance;
            var rows = ctx.Moves.Where(m => m.AccountId == a.Id && m.Date >= a.OpeningDate.Date)
                .OrderBy(m => m.Date).ThenBy(m => m.Source == "invoice" ? 0 : 1).ThenBy(m => m.RefId);
            foreach (var m in rows)
            {
                balance += m.Direction == FinanceDirection.In ? m.Amount : -m.Amount;
                m.BalanceAfter = balance;
            }
            ctx.Balances[a.Id] = balance;
        }
    }

    // ══ OVERVIEW ═════════════════════════════════════════════════════════════

    public FinanceOverviewDto Overview(FinanceContext c, FinancePeriod period, Dictionary<string, string> labels)
    {
        var open = OpenInvoices(c);
        var bills = BillRows(c);
        var onWay = c.Items.Where(i => i.Kind == FinanceItemKind.InTransit && !i.Flag).ToList();

        var dto = new FinanceOverviewDto
        {
            Setting = MapSetting(c.Setting, labels),
            Period = MapPeriod(period),
            Periods = PeriodOptions.Select(p => new FinancePeriodOptionDto { Key = p.Key, Label = p.Label }).ToList(),
            MoneyToUse = c.Accounts.Where(a => a.IsActive).Sum(a => c.Balances[a.Id]),
            AccountCount = c.Accounts.Count(a => a.IsActive),
            OnTheWayTotal = onWay.Sum(i => i.Amount),
            OnTheWayCount = onWay.Count,
            OnTheWayLatestDays = onWay.Where(i => i.Date.HasValue).Select(i => (int?)(i.Date!.Value.Date - c.Today).Days).DefaultIfEmpty().Max(),
            CustomersOwe = open.Sum(d => d.Balance),
            CustomersOweCount = open.Count,
            LateInvoices = open.Count(d => d.DaysLate.HasValue),
            SuppliersOwe = bills.Where(b => b.Remaining > Eps).Sum(b => b.Remaining),
            OpenBills = bills.Count(b => b.Remaining > Eps),
            OverdueBills = bills.Count(b => b.Status == "overdue"),
            ProfitPeriodLabel = period.Label,
            Profit = Aggregate(c, period.From, period.To).Profit,
            Unassigned = Unassigned(c),
            Accounts = c.Accounts.Where(a => a.IsActive).Select(a => MapAccount(c, a)).ToList(),
            Categories = c.Categories.Where(x => x.IsActive).Select(MapCategory).ToList(),
            AllAccounts = c.Accounts.Select(a => MapAccount(c, a)).ToList(),
            AllCategories = c.Categories.Select(MapCategory).ToList(),
            Recurring = c.Recurring.OrderBy(r => r.Order).ThenBy(r => r.Id).Select(r => MapRecurring(c, r)).ToList(),
            Items = c.Items.Where(i => !(i.Kind == FinanceItemKind.InTransit && i.Flag)).Select(MapItem).ToList(),
        };
        return dto;
    }

    private static FinanceUnassignedDto Unassigned(FinanceContext c)
    {
        var floor = c.Accounts.Where(a => a.IsActive).Select(a => (DateTime?)a.OpeningDate.Date).DefaultIfEmpty().Min();
        var lost = c.Moves.Where(m => m.Source == "invoice" && m.AccountId == null && (floor == null || m.Date >= floor)).ToList();
        var methods = c.Payments
            .Where(p => !c.MethodAccount.ContainsKey(p.Method.ToString()))
            .Select(p => p.Method.ToString()).Distinct().OrderBy(x => x).ToList();
        return new FinanceUnassignedDto { Count = lost.Count, Amount = lost.Sum(m => m.Amount), Methods = methods };
    }

    // ══ CASH ═════════════════════════════════════════════════════════════════

    public FinanceCashDto Cash(FinanceContext c, FinancePeriod period, int movementLimit)
    {
        var inPeriod = c.Moves.Where(m => m.AccountId != null && !m.IsTransfer && m.Date >= period.From && m.Date <= period.To).ToList();
        var ins  = inPeriod.Where(m => m.Direction == FinanceDirection.In).ToList();
        var outs = inPeriod.Where(m => m.Direction == FinanceDirection.Out).ToList();

        var latest = c.Moves.Where(m => m.AccountId != null)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.Source == "invoice" ? 0 : 1).ThenByDescending(m => m.RefId)
            .Take(movementLimit).ToList();
        var names = c.Accounts.ToDictionary(a => a.Id, a => a.Name);

        return new FinanceCashDto
        {
            Accounts = c.Accounts.Where(a => a.IsActive).Select(a => MapAccount(c, a)).ToList(),
            OnTheWay = c.Items.Where(i => i.Kind == FinanceItemKind.InTransit && !i.Flag)
                .OrderBy(i => i.Date ?? DateTime.MaxValue).ThenBy(i => i.Order).Select(MapItem).ToList(),
            MoneyIn = ins.Sum(m => m.Amount),
            MoneyOut = outs.Sum(m => m.Amount),
            InLines = Lines(ins),
            OutLines = Lines(outs),
            Movements = latest.Select(m => new FinanceMovementDto
            {
                Source = m.Source, RefId = m.RefId, Date = m.Date, Direction = m.Direction, Title = m.Title, Category = m.Category,
                AccountId = m.AccountId, AccountName = m.AccountId.HasValue && names.TryGetValue(m.AccountId.Value, out var n) ? n : "",
                Amount = m.Amount, BalanceAfter = m.BalanceAfter, IsTransfer = m.IsTransfer,
            }).ToList(),
        };
    }

    /// <summary>Group by category, biggest first; keeps the top four and folds the rest into "Other".</summary>
    private static List<FinanceFlowLineDto> Lines(List<FinanceMove> moves)
    {
        var groups = moves.GroupBy(m => string.IsNullOrWhiteSpace(m.Category) ? "Other" : m.Category!)
            .Select(g => new FinanceFlowLineDto { Label = g.Key, Amount = g.Sum(m => m.Amount) })
            .OrderByDescending(l => l.Amount).ToList();
        if (groups.Count <= 5) return groups;
        var top = groups.Take(4).ToList();
        top.Add(new FinanceFlowLineDto { Label = "Other", Amount = groups.Skip(4).Sum(l => l.Amount) });
        return top;
    }

    // ══ CUSTOMER DUES ════════════════════════════════════════════════════════

    private static readonly SalesInvoiceStatus[] OpenStatuses = { SalesInvoiceStatus.Issued, SalesInvoiceStatus.PartiallyPaid, SalesInvoiceStatus.Overdue };
    private static readonly SalesInvoiceStatus[] SaleStatuses = { SalesInvoiceStatus.Issued, SalesInvoiceStatus.Paid, SalesInvoiceStatus.PartiallyPaid, SalesInvoiceStatus.Overdue };

    private static List<FinanceDueDto> OpenInvoices(FinanceContext c) =>
        c.Invoices.Where(i => OpenStatuses.Contains(i.Status) && Money(i.BalanceDue) > Eps)
            .Select(i => new FinanceDueDto
            {
                InvoiceId = i.Id, InvoiceNumber = i.InvoiceNumber ?? $"#{i.Id}", CustomerName = i.CustomerName ?? "Customer",
                CustomerPhone = i.CustomerPhone, Channel = i.Channel, InvoiceDate = i.InvoiceDate.Date, DueDate = i.DueDate?.Date,
                Balance = Money(i.BalanceDue),
                DaysLate = i.DueDate.HasValue && i.DueDate.Value.Date < c.Today ? (c.Today - i.DueDate.Value.Date).Days : null,
            }).ToList();

    public FinanceDuesDto Dues(FinanceContext c, FinancePeriod period, Dictionary<string, string> labels)
    {
        var open = OpenInvoices(c);
        var s1 = c.Setting.AgingStep1; var s2 = Math.Max(c.Setting.AgingStep2, s1 + 1);
        var late = open.Where(d => d.DaysLate.HasValue).ToList();

        FinanceAgingDto Bucket(string key, string label, Func<FinanceDueDto, bool> pick)
        {
            var rows = open.Where(pick).ToList();
            return new FinanceAgingDto { Key = key, Label = label, Amount = rows.Sum(r => r.Balance), Count = rows.Count };
        }

        var collected = c.Payments.Where(p => p.PaidOn.Date >= period.From && p.PaidOn.Date <= period.To).Sum(p => Money(p.Amount));
        var sales = SalesGross(c, period.From, period.To);

        var dealers = c.Items.Where(i => i.Kind == FinanceItemKind.DealerCredit).Select(i =>
        {
            var inv = open.Where(d => string.Equals(d.CustomerName.Trim(), i.Title.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            var used = inv.Sum(d => d.Balance);
            return new FinanceDealerDto { Id = i.Id, Name = i.Title, Detail = i.Detail, Limit = i.Amount, Used = used, Left = i.Amount - used, OpenInvoices = inv };
        }).ToList();

        var advances = c.Items.Where(i => i.Kind == FinanceItemKind.Advance).OrderBy(i => i.Order).Select(MapItem).ToList();

        return new FinanceDuesDto
        {
            Total = open.Sum(d => d.Balance), Count = open.Count,
            LateAmount = late.Sum(d => d.Balance), LateCount = late.Count,
            Collected = collected,
            CollectedPercentOfSales = sales > Eps ? Math.Round(collected / sales * 100, 0) : null,
            AverageDaysToPay = AverageDaysToPay(c, period.From, period.To),
            PreviousAverageDaysToPay = AverageDaysToPay(c, period.PrevFrom, period.PrevTo),
            Aging = new List<FinanceAgingDto>
            {
                Bucket("current", "Not due yet", d => !d.DaysLate.HasValue),
                Bucket("b1", $"1–{s1} days late", d => d.DaysLate is >= 1 && d.DaysLate <= s1),
                Bucket("b2", $"{s1 + 1}–{s2} days late", d => d.DaysLate > s1 && d.DaysLate <= s2),
                Bucket("b3", $"Over {s2} days", d => d.DaysLate > s2),
            },
            Advances = advances,
            AdvancesTotal = advances.Sum(a => a.Amount),
            Dues = open.OrderByDescending(d => d.Balance).ThenBy(d => d.InvoiceNumber).Take(100).ToList(),
            Dealers = dealers,
        };
    }

    private static double? AverageDaysToPay(FinanceContext c, DateTime from, DateTime to)
    {
        var days = new List<int>();
        foreach (var inv in c.Invoices.Where(i => i.Status == SalesInvoiceStatus.Paid))
        {
            var last = c.Payments.Where(p => p.InvoiceId == inv.Id).Select(p => (DateTime?)p.PaidOn.Date).DefaultIfEmpty().Max();
            if (last.HasValue && last >= from && last <= to)
                days.Add(Math.Max(0, (last.Value - inv.InvoiceDate.Date).Days));
        }
        return days.Count == 0 ? null : Math.Round(days.Average(), 0);
    }

    // ══ SUPPLIER BILLS ═══════════════════════════════════════════════════════

    public List<FinanceBillDto> BillRows(FinanceContext c)
    {
        var names = c.Categories.ToDictionary(x => x.Id, x => x.Name);
        var accountNames = c.Accounts.ToDictionary(a => a.Id, a => a.Name);
        var payments = c.Txs.Where(t => t.Source == FinanceTxSource.SupplierBill && t.SourceId.HasValue && t.Direction == FinanceDirection.Out)
            .GroupBy(t => t.SourceId!.Value).ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ThenBy(t => t.Id).ToList());

        return c.Bills.Select(b =>
        {
            payments.TryGetValue(b.Id, out var pays);
            pays ??= new List<FinanceTransaction>();
            var paid = pays.Sum(p => p.Amount);
            var remaining = Math.Max(0, b.Amount - paid);
            var dueIn = (b.DueDate.Date - c.Today).Days;
            var status = remaining <= Eps ? "paid" : dueIn < 0 ? "overdue" : paid > Eps ? "part" : "open";
            return new FinanceBillDto
            {
                Id = b.Id, Supplier = b.Supplier, Description = b.Description, BillNumber = b.BillNumber, BillDate = b.BillDate.Date,
                DueDate = b.DueDate.Date, Amount = b.Amount, CategoryId = b.CategoryId,
                CategoryName = names.GetValueOrDefault(b.CategoryId, ""), ReceiptUrl = b.ReceiptUrl, Note = b.Note,
                Paid = paid, Remaining = remaining, Status = status, DueInDays = dueIn,
                LastPaidOn = pays.Count == 0 ? null : pays.Max(p => p.Date.Date),
                Payments = pays.Select(p => new FinanceBillPaymentDto
                {
                    Id = p.Id, Date = p.Date.Date, Amount = p.Amount, AccountId = p.AccountId,
                    AccountName = accountNames.GetValueOrDefault(p.AccountId, ""), Reference = p.Reference,
                }).ToList(),
            };
        }).ToList();
    }

    public FinanceBillsDto Bills(FinanceContext c, FinancePeriod period)
    {
        var all = BillRows(c);
        var open = all.Where(b => b.Remaining > Eps).ToList();
        var overdue = open.Where(b => b.Status == "overdue").ToList();
        var soon = open.Where(b => b.Status != "overdue" && b.DueInDays <= c.Setting.DueSoonDays).ToList();
        var periodPays = c.Txs.Where(t => t.Source == FinanceTxSource.SupplierBill && t.Direction == FinanceDirection.Out
            && t.Date.Date >= period.From && t.Date.Date <= period.To).ToList();

        // Show everything still unpaid plus bills settled recently, so paid rows can give a Receipt.
        var recent = c.Today.AddDays(-30);
        var rows = all.Where(b => b.Remaining > Eps || (b.LastPaidOn.HasValue && b.LastPaidOn >= recent && b.LastPaidOn >= period.From.AddDays(-30)))
            .OrderBy(b => b.DueDate).ThenBy(b => b.Id).Take(100).ToList();

        var plan = BuildPlan(c, open);
        var tight = plan.FirstOrDefault(w => w.ToPay > w.Expected);

        return new FinanceBillsDto
        {
            Owe = open.Sum(b => b.Remaining), OpenCount = open.Count,
            DueSoonAmount = soon.Sum(b => b.Remaining), DueSoonCount = soon.Count,
            OverdueAmount = overdue.Sum(b => b.Remaining), OverdueCount = overdue.Count,
            OverdueMaxDays = overdue.Select(b => -b.DueInDays).DefaultIfEmpty(0).Max(),
            PaidInPeriod = periodPays.Sum(t => t.Amount), PaidCount = periodPays.Select(t => t.SourceId).Distinct().Count(),
            Bills = rows, Plan = plan, TightWeek = tight?.Label,
            LettersOfCredit = c.Items.Where(i => i.Kind == FinanceItemKind.LetterOfCredit).OrderBy(i => i.Order).Select(MapItem).ToList(),
        };
    }

    /// <summary>Four weeks ahead: what must be paid (bills + monthly costs) against what is expected in.</summary>
    private static List<FinancePlanWeekDto> BuildPlan(FinanceContext c, List<FinanceBillDto> openBills)
    {
        string[] labels = { "This week", "Next week", "In 3 weeks", "In 4 weeks" };
        var weeks = labels.Select(l => new FinancePlanWeekDto { Label = l }).ToList();
        int WeekOf(DateTime d) => Math.Clamp(((d.Date - c.Today).Days) / 7, 0, 3);
        bool InWindow(DateTime d) => (d.Date - c.Today).Days < 28;

        foreach (var b in openBills)                       // overdue bills are due right now
            if (b.DueInDays < 28) weeks[b.DueInDays < 0 ? 0 : WeekOf(b.DueDate)].ToPay += b.Remaining;

        foreach (var r in c.Recurring.Where(r => r.IsActive))
            foreach (var due in RecurringOccurrences(c, r))
                if (InWindow(due)) weeks[WeekOf(due)].ToPay += r.Amount;

        foreach (var d in OpenInvoices(c).Where(d => d.DueDate.HasValue && d.DueDate.Value >= c.Today && InWindow(d.DueDate.Value)))
            weeks[WeekOf(d.DueDate!.Value)].Expected += d.Balance;     // late invoices are not predicted

        foreach (var i in c.Items.Where(i => i.Kind == FinanceItemKind.InTransit && !i.Flag))
        {
            var when = i.Date?.Date ?? c.Today;
            if (InWindow(when) || when < c.Today) weeks[when < c.Today ? 0 : WeekOf(when)].Expected += i.Amount;
        }
        return weeks;
    }

    /// <summary>Unpaid occurrences of a monthly cost: this month's (if not yet paid) and next month's.</summary>
    private static IEnumerable<DateTime> RecurringOccurrences(FinanceContext c, FinanceRecurringCost r)
    {
        DateTime At(DateTime month) => new(month.Year, month.Month, Math.Min(r.DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)));
        var thisMonth = new DateTime(c.Today.Year, c.Today.Month, 1);
        if (RecurringStatus(c, r) != "paid") yield return At(thisMonth);
        yield return At(thisMonth.AddMonths(1));
    }

    public static string RecurringStatus(FinanceContext c, FinanceRecurringCost r)
    {
        var paid = c.Expenses.Any(e => e.RecurringCostId == r.Id && e.Date.Year == c.Today.Year && e.Date.Month == c.Today.Month);
        if (paid) return "paid";
        var day = Math.Min(r.DayOfMonth, DateTime.DaysInMonth(c.Today.Year, c.Today.Month));
        return day <= c.Today.Day ? "due" : "scheduled";
    }

    // ══ EXPENSES ═════════════════════════════════════════════════════════════

    public FinanceExpensesDto Expenses(FinanceContext c, FinancePeriod period)
    {
        var cats = c.Categories.ToDictionary(x => x.Id);
        var cur = c.Expenses.Where(e => e.Date.Date >= period.From && e.Date.Date <= period.To).ToList();
        var prev = c.Expenses.Where(e => e.Date.Date >= period.PrevFrom && e.Date.Date <= period.PrevTo).ToList();

        var lines = cur.GroupBy(e => e.CategoryId).Select(g =>
        {
            cats.TryGetValue(g.Key, out var cat);
            var amount = g.Sum(e => e.Amount);
            var before = prev.Where(e => e.CategoryId == g.Key).Sum(e => e.Amount);
            return new FinanceCostLineDto
            {
                CategoryId = g.Key, Name = cat?.Name ?? "Other", Color = cat?.Color ?? "#6B7280", Amount = amount, Count = g.Count(),
                ChangePercent = Pct(amount, before),
            };
        }).OrderByDescending(l => l.Amount).ToList();

        var running = cur.Where(e => cats.TryGetValue(e.CategoryId, out var k) && k.CostGroup == FinanceCostGroup.Running).Sum(e => e.Amount);
        var sales = Aggregate(c, period.From, period.To).Net;
        var orders = c.Invoices.Count(i => SaleStatuses.Contains(i.Status) && i.InvoiceDate.Date >= period.From && i.InvoiceDate.Date <= period.To);
        var biggest = lines.FirstOrDefault();
        var upMost = lines.Where(l => l.ChangePercent is > 0).OrderByDescending(l => l.ChangePercent).FirstOrDefault();

        var recurring = c.Recurring.Where(r => r.IsActive).OrderBy(r => r.DayOfMonth).ThenBy(r => r.Order)
            .Select(r => MapRecurring(c, r)).ToList();

        var accountNames = c.Accounts.ToDictionary(a => a.Id, a => a.Name);
        return new FinanceExpensesDto
        {
            Running = running,
            RunningPercentOfSales = sales > Eps ? Math.Round(running / sales * 100, 0) : null,
            BiggestName = biggest?.Name, BiggestAmount = biggest?.Amount ?? 0, BiggestCount = biggest?.Count ?? 0,
            UpMostName = upMost?.Name, UpMostPercent = upMost?.ChangePercent,
            CostPerOrder = orders > 0 ? Math.Round(running / orders, 0) : null, OrderCount = orders,
            TotalSpent = cur.Sum(e => e.Amount),
            Categories = lines, Recurring = recurring, RecurringTotal = recurring.Sum(r => r.Amount),
            Latest = cur.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).Take(8).Select(e => MapExpense(e, cats, accountNames)).ToList(),
            PeriodExpenseCount = cur.Count,
        };
    }

    // ══ PROFIT & LOSS ════════════════════════════════════════════════════════

    public sealed record Aggregated(decimal Gross, decimal Vat, decimal Net,
        Dictionary<string, decimal> CostOfSales, Dictionary<string, decimal> Running)
    {
        public decimal GrossProfit => Net - CostOfSales.Values.Sum();
        public decimal Profit => GrossProfit - Running.Values.Sum();
    }

    private static decimal SalesGross(FinanceContext c, DateTime from, DateTime to) =>
        c.Invoices.Where(i => SaleStatuses.Contains(i.Status) && i.InvoiceDate.Date >= from && i.InvoiceDate.Date <= to).Sum(i => Money(i.GrandTotal));

    /// <summary>
    /// Sales come from invoices dated in the range. Costs are supplier bills (by bill date) and expenses (by date),
    /// rolled into the profit-and-loss line of their category.
    /// </summary>
    public Aggregated Aggregate(FinanceContext c, DateTime from, DateTime to)
    {
        var inv = c.Invoices.Where(i => SaleStatuses.Contains(i.Status) && i.InvoiceDate.Date >= from && i.InvoiceDate.Date <= to).ToList();
        var gross = inv.Sum(i => Money(i.GrandTotal));
        var vat = inv.Sum(i => Money(i.TaxTotal));

        var cats = c.Categories.ToDictionary(x => x.Id);
        var cos = new Dictionary<string, decimal>();
        var run = new Dictionary<string, decimal>();

        void Add(int categoryId, decimal amount)
        {
            if (!cats.TryGetValue(categoryId, out var cat)) { run["Other"] = run.GetValueOrDefault("Other") + amount; return; }
            var target = cat.CostGroup == FinanceCostGroup.CostOfSales ? cos : run;
            target[cat.PlLine] = target.GetValueOrDefault(cat.PlLine) + amount;
        }

        foreach (var b in c.Bills.Where(b => b.BillDate.Date >= from && b.BillDate.Date <= to)) Add(b.CategoryId, b.Amount);
        foreach (var e in c.Expenses.Where(e => e.Date.Date >= from && e.Date.Date <= to)) Add(e.CategoryId, e.Amount);

        return new Aggregated(gross, vat, gross - vat, cos, run);
    }

    public FinancePnlDto Pnl(FinanceContext c, FinancePeriod period)
    {
        var cur = Aggregate(c, period.From, period.To);
        var prev = Aggregate(c, period.PrevFrom, period.PrevTo);
        var lines = new List<FinancePnlLineDto>();

        void Line(string section, string key, string label, decimal current, decimal previous, bool total = false) =>
            lines.Add(new FinancePnlLineDto
            {
                Section = section, Key = key, Label = label, Current = current, Previous = previous, IsTotal = total,
                ChangePercent = Pct(Math.Abs(current), Math.Abs(previous)),
            });

        Line("sales", "gross", "Sales (VAT included)", cur.Gross, prev.Gross);
        Line("sales", "vat", "VAT included in sales", -cur.Vat, -prev.Vat);
        Line("sales", "net", "Sales before VAT", cur.Net, prev.Net, true);

        foreach (var label in cur.CostOfSales.Keys.Union(prev.CostOfSales.Keys).OrderByDescending(k => cur.CostOfSales.GetValueOrDefault(k) + prev.CostOfSales.GetValueOrDefault(k)))
            Line("cost", "cost:" + label, label, -cur.CostOfSales.GetValueOrDefault(label), -prev.CostOfSales.GetValueOrDefault(label));
        Line("cost", "grossProfit", "Gross profit", cur.GrossProfit, prev.GrossProfit, true);

        foreach (var label in cur.Running.Keys.Union(prev.Running.Keys).OrderByDescending(k => cur.Running.GetValueOrDefault(k) + prev.Running.GetValueOrDefault(k)))
            Line("running", "run:" + label, label, -cur.Running.GetValueOrDefault(label), -prev.Running.GetValueOrDefault(label));
        Line("running", "profit", "Profit for the period", cur.Profit, prev.Profit, true);

        // Monthly profit for the chart, ending in the month the period ends (never in the future).
        var anchor = period.To < c.Today ? period.To : c.Today;
        var endMonth = new DateTime(anchor.Year, anchor.Month, 1);
        var n = c.Setting.ChartMonths;
        var bars = Enumerable.Range(0, n).Select(i => endMonth.AddMonths(i - (n - 1))).Select(m =>
        {
            var from = m; var to = m.AddMonths(1).AddDays(-1);
            return new FinanceBarDto
            {
                Label = m.ToString("MMM", CultureInfo.InvariantCulture),
                Profit = Aggregate(c, from, to).Profit,
                IsCurrent = m == endMonth,
            };
        }).ToList();

        return new FinancePnlDto
        {
            Lines = lines, Bars = bars, Insights = Insights(bars),
            Notes = c.Items.Where(i => i.Kind == FinanceItemKind.Insight).OrderBy(i => i.Order).Select(MapItem).ToList(),
        };
    }

    private static List<FinanceInsightDto> Insights(List<FinanceBarDto> bars)
    {
        var result = new List<FinanceInsightDto>();
        if (bars.Count < 3 || bars.All(b => b.Profit == 0)) return result;
        var avg = bars.Average(b => b.Profit);
        var best = bars.OrderByDescending(b => b.Profit).First();
        var worst = bars.OrderBy(b => b.Profit).First();
        if (best.Profit > 0)
            result.Add(new FinanceInsightDto { Kind = "best", Label = best.Label, Amount = best.Profit, PercentVsAverage = avg == 0 ? null : (int)Math.Round((best.Profit - avg) / Math.Abs(avg) * 100) });
        if (worst.Profit < avg && worst.Label != best.Label)
            result.Add(new FinanceInsightDto { Kind = "dip", Label = worst.Label, Amount = worst.Profit, PercentVsAverage = avg == 0 ? null : (int)Math.Round((avg - worst.Profit) / Math.Abs(avg) * 100) });
        return result;
    }

    // ══ MAPPING & HELPERS ════════════════════════════════════════════════════

    public static decimal Money(double v) => Math.Round((decimal)v, 2);

    public static int? Pct(decimal current, decimal previous) =>
        previous == 0 ? null : (int)Math.Round((current - previous) / Math.Abs(previous) * 100);

    public static List<string> SplitCsv(string? s) =>
        (s ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    public static FinancePeriodDto MapPeriod(FinancePeriod p) => new() { Key = p.Key, Label = p.Label, PrevLabel = p.PrevLabel, From = p.From, To = p.To };

    public FinanceAccountDto MapAccount(FinanceContext c, FinanceAccount a)
    {
        var verb = a.Kind == FinanceAccountKind.Cash ? "Counted" : "Matched";
        string text, tone = "green";
        if (!a.LastReconciledOn.HasValue) { text = a.Kind == FinanceAccountKind.Cash ? "Never counted" : "Never matched"; tone = "amber"; }
        else
        {
            var days = (c.Today - a.LastReconciledOn.Value.Date).Days;
            if (days <= 0) text = $"{verb} today";
            else if (days > 3) { text = $"Not {verb.ToLowerInvariant()} {days} days"; tone = "amber"; }
            else text = $"{verb} {a.LastReconciledOn.Value:d MMM}";
        }
        return new FinanceAccountDto
        {
            Id = a.Id, Name = a.Name, Kind = a.Kind, ShortCode = a.ShortCode, Color = a.Color, OpeningBalance = a.OpeningBalance,
            OpeningDate = a.OpeningDate.Date, PaymentMethods = SplitCsv(a.PaymentMethods), LastReconciledOn = a.LastReconciledOn?.Date,
            IsActive = a.IsActive, Order = a.Order, Balance = c.Balances.GetValueOrDefault(a.Id), StatusText = text, StatusTone = tone,
        };
    }

    public static FinanceRecurringDto MapRecurring(FinanceContext c, FinanceRecurringCost r) => new()
    {
        Id = r.Id, Name = r.Name, Detail = r.Detail, DayOfMonth = r.DayOfMonth, Amount = r.Amount, CategoryId = r.CategoryId,
        AccountId = r.AccountId, IsActive = r.IsActive, Order = r.Order, Status = RecurringStatus(c, r),
    };

    public static FinanceCategoryDto MapCategory(FinanceExpenseCategory x) => new()
    {
        Id = x.Id, Name = x.Name, Color = x.Color, CostGroup = x.CostGroup, PlLine = x.PlLine, Order = x.Order, IsActive = x.IsActive,
    };

    public static FinanceItemDto MapItem(FinanceListItem i) => new()
    {
        Id = i.Id, Kind = i.Kind, Title = i.Title, Detail = i.Detail, Extra = i.Extra, Color = i.Color, Amount = i.Amount,
        Date = i.Date?.Date, Percent = i.Percent, Flag = i.Flag, Order = i.Order,
    };

    public static FinanceExpenseDto MapExpense(FinanceExpense e, Dictionary<int, FinanceExpenseCategory> cats, Dictionary<int, string> accounts)
    {
        cats.TryGetValue(e.CategoryId, out var cat);
        var paidBy = e.AccountId.HasValue && accounts.TryGetValue(e.AccountId.Value, out var n)
            ? (string.IsNullOrWhiteSpace(e.PaidByNote) ? n : $"{n} · {e.PaidByNote}")
            : e.PaidByNote ?? "";
        return new FinanceExpenseDto
        {
            Id = e.Id, Date = e.Date.Date, CategoryId = e.CategoryId, CategoryName = cat?.Name ?? "Other", CategoryColor = cat?.Color ?? "#6B7280",
            Description = e.Description, Amount = e.Amount, AccountId = e.AccountId, PaidBy = paidBy, PaidByNote = e.PaidByNote,
            ReceiptUrl = e.ReceiptUrl, RecurringCostId = e.RecurringCostId,
        };
    }

    public static FinanceSettingDto MapSetting(FinanceSetting s, Dictionary<string, string> labels) => new()
    {
        AccentColor = s.AccentColor, CurrencySymbol = s.CurrencySymbol, CompactMoney = s.CompactMoney, DueSoonDays = s.DueSoonDays,
        AgingStep1 = s.AgingStep1, AgingStep2 = s.AgingStep2, ChartMonths = s.ChartMonths,
        ReminderTemplate = string.IsNullOrWhiteSpace(s.ReminderTemplate) ? FinanceDefaults.ReminderTemplate : s.ReminderTemplate,
        Labels = FinanceDefaults.Labels.Select(d => new FinanceLabelDto { Key = d.Key, Group = d.Group, Caption = d.Caption, Value = labels[d.Key] }).ToList(),
    };

    public static Dictionary<string, string> ReadLabels(FinanceSetting s)
    {
        var map = FinanceDefaults.Labels.ToDictionary(d => d.Key, d => d.Default);
        if (string.IsNullOrWhiteSpace(s.LabelsJson)) return map;
        try
        {
            foreach (var (k, v) in JsonSerializer.Deserialize<Dictionary<string, string>>(s.LabelsJson) ?? new())
                if (map.ContainsKey(k)) map[k] = v;
        }
        catch (JsonException) { /* corrupt JSON: fall back to the defaults */ }
        return map;
    }

    // ══ DEFAULTS ═════════════════════════════════════════════════════════════

    public async Task EnsureDefaultsAsync()
    {
        if (await _settings.GetCountAsync() > 0) return;

        await SeedLock.WaitAsync();
        try
        {
            if (await _settings.GetCountAsync() > 0) return;

            await _settings.InsertAsync(new FinanceSetting { LabelsJson = "{}", ReminderTemplate = FinanceDefaults.ReminderTemplate }, autoSave: true);

            var today = _clock.Now.Date;
            if (await _accounts.GetCountAsync() == 0)
            {
                var order = 0;
                await _accounts.InsertManyAsync(FinanceDefaults.Accounts.Select(a => new FinanceAccount
                {
                    Name = a.Name, Kind = a.Kind, ShortCode = a.Code, Color = a.Color, PaymentMethods = a.Methods,
                    OpeningBalance = 0, OpeningDate = today, Order = ++order,
                }), autoSave: true);
            }
            if (await _categories.GetCountAsync() == 0)
            {
                var order = 0;
                await _categories.InsertManyAsync(FinanceDefaults.Categories.Select(k => new FinanceExpenseCategory
                {
                    Name = k.Name, Color = k.Color, CostGroup = k.Group, PlLine = k.PlLine, Order = ++order,
                }), autoSave: true);
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }
}
