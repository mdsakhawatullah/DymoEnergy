using System;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.SalesInvoices;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.Finance;

public abstract class FinanceAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IFinanceAppService _service;
    private static readonly FinancePeriodInput Month = new() { Period = "this-month" };
    private static DateTime Today => DateTime.Today;

    protected FinanceAppService_Tests()
    {
        _service = GetRequiredService<IFinanceAppService>();
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private async Task<int> AddInvoiceAsync(string customer, double grand, double tax, double paid, DateTime? due,
        SalesInvoiceStatus status, params (double amount, SalesInvoicePaymentMethod method)[] payments)
    {
        var id = 0;
        await WithUnitOfWorkAsync(async () =>
        {
            var invoices = GetRequiredService<IRepository<SalesInvoice, int>>();
            var pays = GetRequiredService<IRepository<SalesInvoicePayment, int>>();
            var inv = await invoices.InsertAsync(new SalesInvoice
            {
                InvoiceNumber = $"INV-T-{Guid.NewGuid().ToString()[..6]}", InvoiceDate = Today, DueDate = due, CustomerName = customer,
                GrandTotal = grand, TaxTotal = tax, AmountPaid = paid, BalanceDue = grand - paid, Status = status, Channel = "Showroom",
            }, autoSave: true);
            foreach (var (amount, method) in payments)
                await pays.InsertAsync(new SalesInvoicePayment { InvoiceId = inv.Id, Amount = amount, Method = method, PaidOn = DateTime.Now }, autoSave: true);
            id = inv.Id;
        });
        return id;
    }

    private async Task<FinanceAccountDto> AccountAsync(string name) =>
        (await _service.GetOverviewAsync(Month)).Accounts.First(a => a.Name == name);

    private async Task<int> CategoryIdAsync(string name) =>
        (await _service.GetOverviewAsync(Month)).Categories.First(c => c.Name == name).Id;

    // ── seeding & settings ────────────────────────────────────────────────

    [Fact]
    public async Task Should_Seed_Accounts_And_Categories_Once_With_Zero_Totals()
    {
        var o = await _service.GetOverviewAsync(Month);

        o.Accounts.Select(a => a.Name).ShouldBe(new[] { "Cash in hand", "bKash merchant", "Nagad merchant", "Bank current account" });
        o.Categories.Count.ShouldBe(9);
        o.MoneyToUse.ShouldBe(0m);
        o.CustomersOwe.ShouldBe(0m);
        o.SuppliersOwe.ShouldBe(0m);
        o.Profit.ShouldBe(0m);
        o.Unassigned.Count.ShouldBe(0);
        o.Setting.Labels.First(l => l.Key == "page.title").Value.ShouldBe("Financials & billing");
        o.Periods.Count.ShouldBe(4);

        (await _service.GetOverviewAsync(Month)).Accounts.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Customise_Labels_And_Validate_Thresholds()
    {
        var s = await _service.UpdateSettingAsync(new UpdateFinanceSettingDto
        {
            AccentColor = "#2563EB", CurrencySymbol = "Tk", CompactMoney = false, DueSoonDays = 10, AgingStep1 = 15, AgingStep2 = 45, ChartMonths = 4,
            Labels = new() { ["page.title"] = "Money", ["tab.cash"] = "Bank & cash" },
        });
        s.CurrencySymbol.ShouldBe("Tk");
        s.Labels.First(l => l.Key == "page.title").Value.ShouldBe("Money");
        s.Labels.First(l => l.Key == "tab.dues").Value.ShouldBe("Customer dues");

        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateSettingAsync(new UpdateFinanceSettingDto
        {
            AccentColor = "#000000", CurrencySymbol = "৳", AgingStep1 = 30, AgingStep2 = 30, Labels = new(),
        }));

        var dues = await _service.GetDuesAsync(Month);
        dues.Aging.Select(a => a.Label).ShouldBe(new[] { "Not due yet", "1–15 days late", "16–45 days late", "Over 45 days" });
    }

    // ── customer money (read from invoices) ───────────────────────────────

    [Fact]
    public async Task Should_Read_Customer_Dues_Ageing_And_Collections_From_Invoices()
    {
        await AddInvoiceAsync("Late Ltd", 1000, 150, 600, Today.AddDays(-10), SalesInvoiceStatus.PartiallyPaid, (600, SalesInvoicePaymentMethod.BKash));
        await AddInvoiceAsync("Fresh Co", 2000, 300, 0, Today.AddDays(5), SalesInvoiceStatus.Issued);
        await AddInvoiceAsync("Cash Co", 500, 75, 500, null, SalesInvoiceStatus.Paid, (500, SalesInvoicePaymentMethod.Cash));
        await AddInvoiceAsync("Draft Co", 9999, 0, 0, null, SalesInvoiceStatus.Draft);

        var dues = await _service.GetDuesAsync(Month);
        dues.Total.ShouldBe(2400m);                   // 400 + 2000; paid and draft invoices are not owed
        dues.Count.ShouldBe(2);
        dues.LateAmount.ShouldBe(400m);
        dues.LateCount.ShouldBe(1);
        dues.Collected.ShouldBe(1100m);               // 600 bKash + 500 cash
        dues.Aging.First(a => a.Key == "current").Amount.ShouldBe(2000m);
        dues.Aging.First(a => a.Key == "b1").Amount.ShouldBe(400m);
        dues.Dues[0].CustomerName.ShouldBe("Fresh Co");       // largest first
        dues.Dues.First(d => d.CustomerName == "Late Ltd").DaysLate.ShouldBe(10);
        dues.AverageDaysToPay.ShouldBe(0d);                    // the paid invoice was settled the day it was raised

        var o = await _service.GetOverviewAsync(Month);
        o.CustomersOwe.ShouldBe(2400m);
        o.LateInvoices.ShouldBe(1);

        // Customer payments land in the account that claims their payment method.
        (await AccountAsync("bKash merchant")).Balance.ShouldBe(600m);
        (await AccountAsync("Cash in hand")).Balance.ShouldBe(500m);
        o.MoneyToUse.ShouldBe(1100m);
    }

    [Fact]
    public async Task Should_Warn_About_Customer_Payments_That_Reach_No_Account()
    {
        await AddInvoiceAsync("Bank Co", 1000, 0, 1000, null, SalesInvoiceStatus.Paid, (1000, SalesInvoicePaymentMethod.BankTransfer));
        (await _service.GetOverviewAsync(Month)).MoneyToUse.ShouldBe(1000m);

        var bank = await AccountAsync("Bank current account");
        await _service.UpdateAccountAsync(bank.Id, new CreateUpdateFinanceAccountDto
        {
            Name = bank.Name, Kind = bank.Kind, ShortCode = bank.ShortCode, Color = bank.Color, OpeningDate = bank.OpeningDate,
            PaymentMethods = bank.PaymentMethods, IsActive = false, Order = bank.Order,
        });

        var o = await _service.GetOverviewAsync(Month);
        o.Unassigned.Count.ShouldBe(1);
        o.Unassigned.Amount.ShouldBe(1000m);
        o.Unassigned.Methods.ShouldContain("BankTransfer");
        o.MoneyToUse.ShouldBe(0m);
    }

    [Fact]
    public async Task Should_Not_Let_Two_Accounts_Claim_The_Same_Payment_Method()
    {
        var bkash = await AccountAsync("bKash merchant");
        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateAccountAsync(new CreateUpdateFinanceAccountDto
        {
            Name = "Second bKash", Kind = FinanceAccountKind.MobileWallet, ShortCode = "bK2", Color = "#DB2777", OpeningDate = Today,
            PaymentMethods = new() { "BKash" },
        }));
        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateAccountAsync(bkash.Id, new CreateUpdateFinanceAccountDto
        {
            Name = bkash.Name, Kind = bkash.Kind, ShortCode = bkash.ShortCode, Color = bkash.Color, OpeningDate = bkash.OpeningDate,
            PaymentMethods = new() { "Cash" },
        }));
        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateAccountAsync(new CreateUpdateFinanceAccountDto
        {
            Name = "Odd", Kind = FinanceAccountKind.Bank, ShortCode = "ODD", Color = "#000000", OpeningDate = Today, PaymentMethods = new() { "Bitcoin" },
        }));
    }

    // ── supplier bills ────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Track_Supplier_Bills_Payments_And_Account_Balances()
    {
        var bank = await AccountAsync("Bank current account");
        await _service.CreateTransactionAsync(new CreateFinanceTransactionDto { Direction = FinanceDirection.In, AccountId = bank.Id, Amount = 5000, Category = "Loan", Description = "Top up" });
        var product = await CategoryIdAsync("Product cost");

        var bill = await _service.CreateBillAsync(new CreateUpdateFinanceBillDto
        {
            Supplier = "Supplier A", BillNumber = "B-1", BillDate = Today, DueDate = Today.AddDays(3), Amount = 1000, CategoryId = product,
        });
        bill.Status.ShouldBe("open");
        bill.Remaining.ShouldBe(1000m);

        var part = await _service.CreateBillPaymentAsync(bill.Id, new PayFinanceBillDto { Amount = 400, AccountId = bank.Id, Reference = "CHQ-1" });
        part.Status.ShouldBe("part");
        part.Paid.ShouldBe(400m);
        part.Remaining.ShouldBe(600m);
        (await AccountAsync("Bank current account")).Balance.ShouldBe(4600m);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateBillPaymentAsync(bill.Id, new PayFinanceBillDto { Amount = 700, AccountId = bank.Id }));
        await Should.ThrowAsync<UserFriendlyException>(() => _service.UpdateBillAsync(bill.Id, new CreateUpdateFinanceBillDto
        {
            Supplier = "Supplier A", BillDate = Today, DueDate = Today.AddDays(3), Amount = 300, CategoryId = product,
        }));
        await Should.ThrowAsync<UserFriendlyException>(() => _service.DeleteBillAsync(bill.Id));

        var paid = await _service.CreateBillPaymentAsync(bill.Id, new PayFinanceBillDto { Amount = 600, AccountId = bank.Id });
        paid.Status.ShouldBe("paid");
        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateBillPaymentAsync(bill.Id, new PayFinanceBillDto { Amount = 1, AccountId = bank.Id }));
        (await AccountAsync("Bank current account")).Balance.ShouldBe(4000m);

        var bills = await _service.GetBillsAsync(Month);
        bills.Owe.ShouldBe(0m);
        bills.PaidInPeriod.ShouldBe(1000m);
        bills.Bills.Single().Status.ShouldBe("paid");

        // Deleting a payment puts the money back and re-opens the balance.
        var reopened = await _service.DeleteBillPaymentAsync(bill.Id, paid.Payments.Last().Id);
        reopened.Status.ShouldBe("part");
        (await AccountAsync("Bank current account")).Balance.ShouldBe(4600m);
    }

    [Fact]
    public async Task Should_Flag_Overdue_Bills_And_A_Tight_Payment_Week()
    {
        var product = await CategoryIdAsync("Product cost");
        await _service.CreateBillAsync(new CreateUpdateFinanceBillDto { Supplier = "Late Supplier", BillDate = Today.AddDays(-20), DueDate = Today.AddDays(-4), Amount = 300, CategoryId = product });
        await _service.CreateBillAsync(new CreateUpdateFinanceBillDto { Supplier = "Next Week", BillDate = Today, DueDate = Today.AddDays(8), Amount = 5000, CategoryId = product });
        await _service.CreateBillAsync(new CreateUpdateFinanceBillDto { Supplier = "Soon", BillDate = Today, DueDate = Today.AddDays(2), Amount = 100, CategoryId = product });

        var bills = await _service.GetBillsAsync(Month);
        bills.Owe.ShouldBe(5400m);
        bills.OverdueCount.ShouldBe(1);
        bills.OverdueAmount.ShouldBe(300m);
        bills.OverdueMaxDays.ShouldBe(4);
        bills.DueSoonCount.ShouldBe(1);
        bills.DueSoonAmount.ShouldBe(100m);
        bills.Plan[0].ToPay.ShouldBe(400m);           // the overdue bill is due right now
        bills.Plan[1].ToPay.ShouldBe(5000m);
        bills.TightWeek.ShouldBe("This week");        // 400 to pay, nothing expected

        // A big customer payment due this week makes the first week fine, but next week is still short.
        await AddInvoiceAsync("Big Co", 10000, 0, 0, Today.AddDays(1), SalesInvoiceStatus.Issued);
        bills = await _service.GetBillsAsync(Month);
        bills.Plan[0].Expected.ShouldBe(10000m);
        bills.TightWeek.ShouldBe("Next week");

        (await _service.GetOverviewAsync(Month)).OverdueBills.ShouldBe(1);
    }

    // ── expenses, recurring costs, transfers ──────────────────────────────

    [Fact]
    public async Task Should_Keep_Expenses_And_The_Ledger_In_Step()
    {
        var cash = await AccountAsync("Cash in hand");
        await _service.CreateTransactionAsync(new CreateFinanceTransactionDto { Direction = FinanceDirection.In, AccountId = cash.Id, Amount = 1000, Category = "Float" });
        var transport = await CategoryIdAsync("Transport");

        var e = await _service.CreateExpenseAsync(new CreateUpdateFinanceExpenseDto
        {
            Date = Today, CategoryId = transport, Description = "Van fuel", Amount = 200, AccountId = cash.Id, PaidByNote = "Admin",
        });
        e.PaidBy.ShouldBe("Cash in hand · Admin");
        (await AccountAsync("Cash in hand")).Balance.ShouldBe(800m);

        await _service.UpdateExpenseAsync(e.Id, new CreateUpdateFinanceExpenseDto { Date = Today, CategoryId = transport, Description = "Van fuel", Amount = 250, AccountId = cash.Id });
        (await AccountAsync("Cash in hand")).Balance.ShouldBe(750m);

        // Switching to "paid some other way" removes the money-out row.
        await _service.UpdateExpenseAsync(e.Id, new CreateUpdateFinanceExpenseDto { Date = Today, CategoryId = transport, Description = "Van fuel", Amount = 250, PaidByNote = "Owner's pocket" });
        (await AccountAsync("Cash in hand")).Balance.ShouldBe(1000m);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.DeleteCategoryAsync(transport));
        await _service.DeleteExpenseAsync(e.Id);
        (await _service.GetExpensesAsync(Month)).PeriodExpenseCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Record_A_Monthly_Cost_Once_Per_Month_And_Report_Expense_Stats()
    {
        var bank = await AccountAsync("Bank current account");
        var rent = await CategoryIdAsync("Rent");
        var salaries = await CategoryIdAsync("Salaries");
        var recurring = await _service.CreateRecurringAsync(new CreateUpdateFinanceRecurringDto { Name = "Warehouse rent", DayOfMonth = 1, Amount = 85000, CategoryId = rent, AccountId = bank.Id });
        recurring.Status.ShouldBe("due");

        var paid = await _service.CreateRecurringPaymentAsync(recurring.Id, new PayRecurringDto());
        paid.Description.ShouldBe("Warehouse rent");
        paid.Amount.ShouldBe(85000m);
        paid.RecurringCostId.ShouldBe(recurring.Id);
        (await AccountAsync("Bank current account")).Balance.ShouldBe(-85000m);
        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateRecurringPaymentAsync(recurring.Id, new PayRecurringDto()));

        await _service.CreateExpenseAsync(new CreateUpdateFinanceExpenseDto { Date = Today, CategoryId = salaries, Description = "Salaries", Amount = 486000 });
        await AddInvoiceAsync("Sales Co", 1000000, 150000, 0, null, SalesInvoiceStatus.Issued);
        await AddInvoiceAsync("Sales Co 2", 1000000, 150000, 0, null, SalesInvoiceStatus.Issued);

        var x = await _service.GetExpensesAsync(Month);
        x.Running.ShouldBe(571000m);
        x.RunningPercentOfSales.ShouldBe(Math.Round(571000m / 1700000m * 100, 0));
        x.BiggestName.ShouldBe("Salaries");
        x.BiggestAmount.ShouldBe(486000m);
        x.OrderCount.ShouldBe(2);
        x.CostPerOrder.ShouldBe(Math.Round(571000m / 2, 0));
        x.Recurring.Single().Status.ShouldBe("paid");
        x.RecurringTotal.ShouldBe(85000m);
        x.Latest.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Move_Money_Between_Accounts_Without_Counting_It_As_Income_Or_Cost()
    {
        var cash = await AccountAsync("Cash in hand");
        var bank = await AccountAsync("Bank current account");
        await _service.CreateTransactionAsync(new CreateFinanceTransactionDto { Direction = FinanceDirection.In, AccountId = cash.Id, Amount = 3000, Category = "Float" });

        await _service.CreateTransferAsync(new CreateFinanceTransferDto { FromAccountId = cash.Id, ToAccountId = bank.Id, Amount = 1200 });
        (await AccountAsync("Cash in hand")).Balance.ShouldBe(1800m);
        (await AccountAsync("Bank current account")).Balance.ShouldBe(1200m);
        (await _service.GetOverviewAsync(Month)).MoneyToUse.ShouldBe(3000m);   // unchanged: it only moved

        var cashTab = await _service.GetCashAsync(Month);
        cashTab.MoneyIn.ShouldBe(3000m);              // the transfer is neither money in…
        cashTab.MoneyOut.ShouldBe(0m);                // …nor money out
        var transferOut = cashTab.Movements.First(m => m.IsTransfer && m.Direction == FinanceDirection.Out);
        transferOut.BalanceAfter.ShouldBe(1800m);

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateTransferAsync(new CreateFinanceTransferDto { FromAccountId = cash.Id, ToAccountId = cash.Id, Amount = 1 }));

        await _service.DeleteTransactionAsync(transferOut.RefId);            // removes both halves
        (await AccountAsync("Cash in hand")).Balance.ShouldBe(3000m);
        (await AccountAsync("Bank current account")).Balance.ShouldBe(0m);
    }

    [Fact]
    public async Task Should_Receive_Money_On_The_Way_Into_An_Account_Once()
    {
        var bkash = await AccountAsync("bKash merchant");
        var item = await _service.CreateItemAsync(new CreateUpdateFinanceItemDto
        {
            Kind = FinanceItemKind.InTransit, Title = "Card settlement", Amount = 154300, Date = Today.AddDays(2), Extra = "CARD",
        });
        var o = await _service.GetOverviewAsync(Month);
        o.OnTheWayTotal.ShouldBe(154300m);
        o.OnTheWayCount.ShouldBe(1);
        o.OnTheWayLatestDays.ShouldBe(2);

        await _service.CreateItemReceiptAsync(item.Id, new ReceiveInTransitDto { AccountId = bkash.Id });
        (await AccountAsync("bKash merchant")).Balance.ShouldBe(154300m);
        (await _service.GetOverviewAsync(Month)).OnTheWayTotal.ShouldBe(0m);
        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateItemReceiptAsync(item.Id, new ReceiveInTransitDto { AccountId = bkash.Id }));
    }

    [Fact]
    public async Task Should_Mark_An_Account_As_Matched_And_Report_Its_State()
    {
        var cash = await AccountAsync("Cash in hand");
        cash.StatusText.ShouldBe("Never counted");
        cash.StatusTone.ShouldBe("amber");

        var done = await _service.UpdateAccountReconciledAsync(cash.Id);
        done.StatusText.ShouldBe("Counted today");
        done.StatusTone.ShouldBe("green");
    }

    // ── accountant export ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_Export_A_Zip_Of_Csv_Files_That_Spreadsheets_Cannot_Run_As_Formulas()
    {
        var cash = await AccountAsync("Cash in hand");
        await _service.CreateTransactionAsync(new CreateFinanceTransactionDto
        {
            Direction = FinanceDirection.In, AccountId = cash.Id, Amount = 100, Category = "Float", Description = "=HYPERLINK(\"http://evil\",\"x\"), \"quoted\"",
        });

        var (stream, fileName) = await GetRequiredService<IFinanceExportService>().BuildAsync("this-month");
        fileName.ShouldStartWith("accountant-export-this-month-");

        using var zip = new System.IO.Compression.ZipArchive(stream);
        zip.Entries.Select(e => e.FullName).OrderBy(x => x, StringComparer.Ordinal).ShouldBe(new[]
            { "README.txt", "expenses.csv", "ledger.csv", "profit-and-loss.csv", "sales-invoices.csv", "supplier-bills.csv" });

        using var reader = new System.IO.StreamReader(zip.GetEntry("ledger.csv")!.Open());
        var ledger = await reader.ReadToEndAsync();
        ledger.ShouldContain("Money in");
        ledger.ShouldContain("100.00");
        ledger.ShouldContain("\"'=HYPERLINK(\"\"http://evil\"\",\"\"x\"\"), \"\"quoted\"\"\"");   // quoted, escaped and neutralised
    }

    // ── profit & loss ─────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Build_Profit_And_Loss_From_Sales_Bills_And_Expenses()
    {
        await AddInvoiceAsync("Buyer", 1000, 150, 0, null, SalesInvoiceStatus.Issued);
        await AddInvoiceAsync("Refunded", 7777, 100, 0, null, SalesInvoiceStatus.Refunded);   // never counted as sales
        var product = await CategoryIdAsync("Product cost");
        await _service.CreateBillAsync(new CreateUpdateFinanceBillDto { Supplier = "S", BillDate = Today, DueDate = Today.AddDays(10), Amount = 300, CategoryId = product });
        await _service.CreateExpenseAsync(new CreateUpdateFinanceExpenseDto { Date = Today, CategoryId = await CategoryIdAsync("Rent"), Description = "Rent", Amount = 100 });
        await _service.CreateExpenseAsync(new CreateUpdateFinanceExpenseDto { Date = Today, CategoryId = await CategoryIdAsync("Utilities"), Description = "Power", Amount = 50 });

        var pnl = await _service.GetPnlAsync(Month);
        decimal Cur(string key) => pnl.Lines.First(l => l.Key == key).Current;

        Cur("gross").ShouldBe(1000m);
        Cur("vat").ShouldBe(-150m);
        Cur("net").ShouldBe(850m);
        Cur("cost:Product cost").ShouldBe(-300m);
        Cur("grossProfit").ShouldBe(550m);
        Cur("run:Rent & utilities").ShouldBe(-150m);     // two categories share one statement line
        Cur("profit").ShouldBe(400m);
        pnl.Lines.First(l => l.Key == "profit").ChangePercent.ShouldBeNull();   // nothing in the previous period to compare with
        pnl.Bars.Count.ShouldBe(6);
        pnl.Bars.Last().IsCurrent.ShouldBeTrue();
        pnl.Bars.Last().Profit.ShouldBe(400m);

        (await _service.GetOverviewAsync(Month)).Profit.ShouldBe(400m);
    }
}
