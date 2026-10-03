using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.SalesInvoices;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Finance;

[Authorize(DymoEnergyPermissions.Finance.Default)]
public class FinanceAppService : ApplicationService, IFinanceAppService
{
    private readonly FinanceCalculator _calc;
    private readonly IRepository<FinanceSetting, int>         _settings;
    private readonly IRepository<FinanceAccount, int>         _accounts;
    private readonly IRepository<FinanceTransaction, int>     _txs;
    private readonly IRepository<FinanceExpenseCategory, int> _categories;
    private readonly IRepository<FinanceExpense, int>         _expenses;
    private readonly IRepository<FinanceSupplierBill, int>    _bills;
    private readonly IRepository<FinanceRecurringCost, int>   _recurring;
    private readonly IRepository<FinanceListItem, int>        _items;

    public FinanceAppService(
        FinanceCalculator calc,
        IRepository<FinanceSetting, int> settings, IRepository<FinanceAccount, int> accounts,
        IRepository<FinanceTransaction, int> txs, IRepository<FinanceExpenseCategory, int> categories,
        IRepository<FinanceExpense, int> expenses, IRepository<FinanceSupplierBill, int> bills,
        IRepository<FinanceRecurringCost, int> recurring, IRepository<FinanceListItem, int> items)
    {
        _calc = calc; _settings = settings; _accounts = accounts; _txs = txs; _categories = categories;
        _expenses = expenses; _bills = bills; _recurring = recurring; _items = items;
    }

    private const decimal Eps = 0.005m;

    // ══ PAGE VIEWS ═══════════════════════════════════════════════════════════

    public async Task<FinanceOverviewDto> GetOverviewAsync(FinancePeriodInput input)
    {
        var c = await _calc.LoadAsync();
        return _calc.Overview(c, FinanceCalculator.ResolvePeriod(input.Period, c.Today), FinanceCalculator.ReadLabels(c.Setting));
    }

    public async Task<FinanceCashDto> GetCashAsync(FinancePeriodInput input)
    {
        var c = await _calc.LoadAsync();
        return _calc.Cash(c, FinanceCalculator.ResolvePeriod(input.Period, c.Today), 8);
    }

    public async Task<FinanceDuesDto> GetDuesAsync(FinancePeriodInput input)
    {
        var c = await _calc.LoadAsync();
        return _calc.Dues(c, FinanceCalculator.ResolvePeriod(input.Period, c.Today), FinanceCalculator.ReadLabels(c.Setting));
    }

    public async Task<FinanceBillsDto> GetBillsAsync(FinancePeriodInput input)
    {
        var c = await _calc.LoadAsync();
        return _calc.Bills(c, FinanceCalculator.ResolvePeriod(input.Period, c.Today));
    }

    public async Task<FinanceExpensesDto> GetExpensesAsync(FinancePeriodInput input)
    {
        var c = await _calc.LoadAsync();
        return _calc.Expenses(c, FinanceCalculator.ResolvePeriod(input.Period, c.Today));
    }

    public async Task<FinancePnlDto> GetPnlAsync(FinancePeriodInput input)
    {
        var c = await _calc.LoadAsync();
        return _calc.Pnl(c, FinanceCalculator.ResolvePeriod(input.Period, c.Today));
    }

    // ══ SETTINGS ═════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceSettingDto> UpdateSettingAsync(UpdateFinanceSettingDto input)
    {
        if (input.AgingStep2 <= input.AgingStep1)
            throw new UserFriendlyException("The second ageing bucket must end later than the first.");

        await _calc.EnsureDefaultsAsync();
        var s = (await _settings.GetListAsync()).OrderBy(x => x.Id).First();
        s.AccentColor = input.AccentColor; s.CurrencySymbol = input.CurrencySymbol.Trim(); s.CompactMoney = input.CompactMoney;
        s.DueSoonDays = input.DueSoonDays; s.AgingStep1 = input.AgingStep1; s.AgingStep2 = input.AgingStep2; s.ChartMonths = input.ChartMonths;
        s.ReminderTemplate = Clean(input.ReminderTemplate);

        var overrides = new Dictionary<string, string>();
        foreach (var def in FinanceDefaults.Labels)
            if (input.Labels.TryGetValue(def.Key, out var v) && v != def.Default && !(string.IsNullOrWhiteSpace(v) && def.Default.Length > 0))
                overrides[def.Key] = v;
        s.LabelsJson = JsonSerializer.Serialize(overrides);

        await _settings.UpdateAsync(s, autoSave: true);
        return FinanceCalculator.MapSetting(s, FinanceCalculator.ReadLabels(s));
    }

    // ══ ACCOUNTS ═════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceAccountDto> CreateAccountAsync(CreateUpdateFinanceAccountDto input)
    {
        await _calc.EnsureDefaultsAsync();
        var a = new FinanceAccount();
        await ApplyAccountAsync(a, input, null);
        if (input.Order <= 0) a.Order = (await _accounts.GetListAsync()).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
        await _accounts.InsertAsync(a, autoSave: true);
        return await AccountDtoAsync(a.Id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceAccountDto> UpdateAccountAsync(int id, CreateUpdateFinanceAccountDto input)
    {
        var a = await _accounts.GetAsync(id);
        await ApplyAccountAsync(a, input, id);
        await _accounts.UpdateAsync(a, autoSave: true);
        return await AccountDtoAsync(id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteAccountAsync(int id)
    {
        if (await _txs.AnyAsync(t => t.AccountId == id) || await _expenses.AnyAsync(e => e.AccountId == id) || await _recurring.AnyAsync(r => r.AccountId == id))
            throw new UserFriendlyException("This account has money movements. Switch it off instead of deleting it, so your history stays correct.");
        await _accounts.DeleteAsync(id, autoSave: true);
    }

    /// <summary>"Mark as matched": the owner confirms the balance agrees with the bank statement or the cash count.</summary>
    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceAccountDto> UpdateAccountReconciledAsync(int id)
    {
        var a = await _accounts.GetAsync(id);
        a.LastReconciledOn = Clock.Now.Date;
        await _accounts.UpdateAsync(a, autoSave: true);
        return await AccountDtoAsync(id);
    }

    private async Task ApplyAccountAsync(FinanceAccount a, CreateUpdateFinanceAccountDto i, int? selfId)
    {
        var methods = new List<string>();
        foreach (var raw in i.PaymentMethods.Select(m => m.Trim()).Where(m => m.Length > 0))
        {
            if (!Enum.TryParse<SalesInvoicePaymentMethod>(raw, true, out var parsed))
                throw new UserFriendlyException($"“{raw}” is not a payment method. Use: {string.Join(", ", Enum.GetNames<SalesInvoicePaymentMethod>())}.");
            methods.Add(parsed.ToString());
        }

        if (i.IsActive)
        {
            foreach (var other in (await _accounts.GetListAsync()).Where(x => x.IsActive && x.Id != selfId))
            {
                var clash = FinanceCalculator.SplitCsv(other.PaymentMethods).Intersect(methods).FirstOrDefault();
                if (clash != null)
                    throw new UserFriendlyException($"“{clash}” payments already go to “{other.Name}”. A payment method can land in only one account.");
            }
        }

        a.Name = i.Name.Trim(); a.Kind = i.Kind; a.ShortCode = i.ShortCode.Trim(); a.Color = i.Color;
        a.OpeningBalance = Math.Round(i.OpeningBalance, 2); a.OpeningDate = i.OpeningDate.Date;
        a.PaymentMethods = methods.Count == 0 ? null : string.Join(",", methods.Distinct());
        a.IsActive = i.IsActive;
        if (i.Order > 0) a.Order = i.Order;
    }

    private async Task<FinanceAccountDto> AccountDtoAsync(int id)
    {
        var c = await _calc.LoadAsync();
        return _calc.MapAccount(c, c.Accounts.First(a => a.Id == id));
    }

    // ══ CATEGORIES ═══════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceCategoryDto> CreateCategoryAsync(CreateUpdateFinanceCategoryDto input)
    {
        await _calc.EnsureDefaultsAsync();
        var k = new FinanceExpenseCategory();
        ApplyCategory(k, input);
        if (input.Order <= 0) k.Order = (await _categories.GetListAsync()).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
        await _categories.InsertAsync(k, autoSave: true);
        return FinanceCalculator.MapCategory(k);
    }

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceCategoryDto> UpdateCategoryAsync(int id, CreateUpdateFinanceCategoryDto input)
    {
        var k = await _categories.GetAsync(id);
        ApplyCategory(k, input);
        await _categories.UpdateAsync(k, autoSave: true);
        return FinanceCalculator.MapCategory(k);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteCategoryAsync(int id)
    {
        if (await _expenses.AnyAsync(e => e.CategoryId == id) || await _bills.AnyAsync(b => b.CategoryId == id) || await _recurring.AnyAsync(r => r.CategoryId == id))
            throw new UserFriendlyException("This category is used by expenses, bills or monthly costs. Switch it off instead of deleting it.");
        await _categories.DeleteAsync(id, autoSave: true);
    }

    private static void ApplyCategory(FinanceExpenseCategory k, CreateUpdateFinanceCategoryDto i)
    {
        k.Name = i.Name.Trim(); k.Color = i.Color; k.CostGroup = i.CostGroup;
        k.PlLine = string.IsNullOrWhiteSpace(i.PlLine) ? k.Name : i.PlLine.Trim();
        k.IsActive = i.IsActive;
        if (i.Order > 0) k.Order = i.Order;
    }

    // ══ RECURRING COSTS ══════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceRecurringDto> CreateRecurringAsync(CreateUpdateFinanceRecurringDto input)
    {
        await _calc.EnsureDefaultsAsync();
        await EnsureCategoryAsync(input.CategoryId);
        await EnsureAccountOrNullAsync(input.AccountId);
        var r = new FinanceRecurringCost();
        ApplyRecurring(r, input);
        if (input.Order <= 0) r.Order = (await _recurring.GetListAsync()).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;
        await _recurring.InsertAsync(r, autoSave: true);
        return await RecurringDtoAsync(r.Id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceRecurringDto> UpdateRecurringAsync(int id, CreateUpdateFinanceRecurringDto input)
    {
        await EnsureCategoryAsync(input.CategoryId);
        await EnsureAccountOrNullAsync(input.AccountId);
        var r = await _recurring.GetAsync(id);
        ApplyRecurring(r, input);
        await _recurring.UpdateAsync(r, autoSave: true);
        return await RecurringDtoAsync(id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteRecurringAsync(int id) => await _recurring.DeleteAsync(id, autoSave: true);

    /// <summary>Records this month's payment of a monthly cost as an expense (and the money-out row if an account is chosen).</summary>
    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task<FinanceExpenseDto> CreateRecurringPaymentAsync(int id, PayRecurringDto input)
    {
        var r = await _recurring.GetAsync(id);
        var date = (input.Date ?? Clock.Now).Date;
        if (await _expenses.AnyAsync(e => e.RecurringCostId == id && e.Date.Year == date.Year && e.Date.Month == date.Month))
            throw new UserFriendlyException($"“{r.Name}” is already recorded for {date:MMMM yyyy}.");

        return await CreateExpenseCoreAsync(new CreateUpdateFinanceExpenseDto
        {
            Date = date, CategoryId = r.CategoryId, Description = r.Name, Amount = input.Amount ?? r.Amount,
            AccountId = input.AccountId ?? r.AccountId,
        }, id);
    }

    private static void ApplyRecurring(FinanceRecurringCost r, CreateUpdateFinanceRecurringDto i)
    {
        r.Name = i.Name.Trim(); r.Detail = Clean(i.Detail); r.DayOfMonth = i.DayOfMonth; r.Amount = Math.Round(i.Amount, 2);
        r.CategoryId = i.CategoryId; r.AccountId = i.AccountId; r.IsActive = i.IsActive;
        if (i.Order > 0) r.Order = i.Order;
    }

    private async Task<FinanceRecurringDto> RecurringDtoAsync(int id)
    {
        var c = await _calc.LoadAsync();
        var r = c.Recurring.First(x => x.Id == id);
        return new FinanceRecurringDto
        {
            Id = r.Id, Name = r.Name, Detail = r.Detail, DayOfMonth = r.DayOfMonth, Amount = r.Amount, CategoryId = r.CategoryId,
            AccountId = r.AccountId, IsActive = r.IsActive, Order = r.Order, Status = FinanceCalculator.RecurringStatus(c, r),
        };
    }

    // ══ GENERIC LIST ITEMS ═══════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceItemDto> CreateItemAsync(CreateUpdateFinanceItemDto input)
    {
        var item = new FinanceListItem { Kind = input.Kind };
        ApplyItem(item, input);
        if (input.Order <= 0)
            item.Order = (await _items.GetListAsync(i => i.Kind == input.Kind)).Select(i => i.Order).DefaultIfEmpty(0).Max() + 1;
        await _items.InsertAsync(item, autoSave: true);
        return FinanceCalculator.MapItem(item);
    }

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceItemDto> UpdateItemAsync(int id, CreateUpdateFinanceItemDto input)
    {
        var item = await _items.GetAsync(id);
        ApplyItem(item, input);        // the kind never changes after creation
        await _items.UpdateAsync(item, autoSave: true);
        return FinanceCalculator.MapItem(item);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteItemAsync(int id) => await _items.DeleteAsync(id, autoSave: true);

    /// <summary>Money on the way has arrived: put it into an account and take it off the list.</summary>
    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task CreateItemReceiptAsync(int id, ReceiveInTransitDto input)
    {
        var item = await _items.GetAsync(id);
        if (item.Kind != FinanceItemKind.InTransit || item.Flag)
            throw new UserFriendlyException("This item is not waiting to be received.");
        if (item.Amount <= 0) throw new UserFriendlyException("Set an amount on the item first.");
        await EnsureAccountAsync(input.AccountId);

        await _txs.InsertAsync(new FinanceTransaction
        {
            Date = (input.Date ?? Clock.Now).Date, AccountId = input.AccountId, Direction = FinanceDirection.In, Amount = item.Amount,
            Category = "Money on the way", Description = item.Title, Source = FinanceTxSource.InTransit, SourceId = item.Id,
        }, autoSave: true);
        item.Flag = true;
        await _items.UpdateAsync(item, autoSave: true);
    }

    private static void ApplyItem(FinanceListItem item, CreateUpdateFinanceItemDto i)
    {
        item.Title = i.Title.Trim(); item.Detail = Clean(i.Detail); item.Extra = Clean(i.Extra); item.Color = Clean(i.Color);
        item.Amount = Math.Round(i.Amount, 2); item.Date = i.Date?.Date; item.Percent = i.Percent; item.Flag = i.Flag;
        if (i.Order > 0) item.Order = i.Order;
    }

    // ══ EXPENSES ═════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public Task<FinanceExpenseDto> CreateExpenseAsync(CreateUpdateFinanceExpenseDto input) => CreateExpenseCoreAsync(input, null);

    private async Task<FinanceExpenseDto> CreateExpenseCoreAsync(CreateUpdateFinanceExpenseDto input, int? recurringId)
    {
        await _calc.EnsureDefaultsAsync();
        var cat = await EnsureCategoryAsync(input.CategoryId);
        await EnsureAccountOrNullAsync(input.AccountId);

        var e = new FinanceExpense { RecurringCostId = recurringId };
        ApplyExpense(e, input);
        await _expenses.InsertAsync(e, autoSave: true);
        await SyncExpenseTxAsync(e, cat);
        return await ExpenseDtoAsync(e.Id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceExpenseDto> UpdateExpenseAsync(int id, CreateUpdateFinanceExpenseDto input)
    {
        var cat = await EnsureCategoryAsync(input.CategoryId);
        await EnsureAccountOrNullAsync(input.AccountId);
        var e = await _expenses.GetAsync(id);
        ApplyExpense(e, input);
        await _expenses.UpdateAsync(e, autoSave: true);
        await SyncExpenseTxAsync(e, cat);
        return await ExpenseDtoAsync(id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteExpenseAsync(int id)
    {
        await _txs.DeleteAsync(t => t.Source == FinanceTxSource.Expense && t.SourceId == id);
        await _expenses.DeleteAsync(id, autoSave: true);
    }

    private static void ApplyExpense(FinanceExpense e, CreateUpdateFinanceExpenseDto i)
    {
        e.Date = i.Date.Date; e.CategoryId = i.CategoryId; e.Description = i.Description.Trim(); e.Amount = Math.Round(i.Amount, 2);
        e.AccountId = i.AccountId; e.PaidByNote = Clean(i.PaidByNote); e.ReceiptUrl = Clean(i.ReceiptUrl);
    }

    /// <summary>Keeps exactly one money-out row in step with an expense that was paid from an account.</summary>
    private async Task SyncExpenseTxAsync(FinanceExpense e, FinanceExpenseCategory cat)
    {
        var tx = await _txs.FirstOrDefaultAsync(t => t.Source == FinanceTxSource.Expense && t.SourceId == e.Id);
        if (!e.AccountId.HasValue)
        {
            if (tx != null) await _txs.DeleteAsync(tx, autoSave: true);
            return;
        }
        tx ??= new FinanceTransaction { Source = FinanceTxSource.Expense, SourceId = e.Id, Direction = FinanceDirection.Out };
        tx.Date = e.Date; tx.AccountId = e.AccountId.Value; tx.Amount = e.Amount; tx.Category = cat.Name; tx.Description = e.Description;
        if (tx.Id == 0) await _txs.InsertAsync(tx, autoSave: true); else await _txs.UpdateAsync(tx, autoSave: true);
    }

    private async Task<FinanceExpenseDto> ExpenseDtoAsync(int id)
    {
        var e = await _expenses.GetAsync(id);
        var cats = (await _categories.GetListAsync()).ToDictionary(x => x.Id);
        var accounts = (await _accounts.GetListAsync()).ToDictionary(a => a.Id, a => a.Name);
        return FinanceCalculator.MapExpense(e, cats, accounts);
    }

    // ══ SUPPLIER BILLS ═══════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task<FinanceBillDto> CreateBillAsync(CreateUpdateFinanceBillDto input)
    {
        await _calc.EnsureDefaultsAsync();
        await EnsureCategoryAsync(input.CategoryId);
        if (input.DueDate.Date < input.BillDate.Date) throw new UserFriendlyException("The due date cannot be before the bill date.");
        var b = new FinanceSupplierBill();
        ApplyBill(b, input);
        await _bills.InsertAsync(b, autoSave: true);
        return await BillDtoAsync(b.Id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Edit)]
    public async Task<FinanceBillDto> UpdateBillAsync(int id, CreateUpdateFinanceBillDto input)
    {
        await EnsureCategoryAsync(input.CategoryId);
        if (input.DueDate.Date < input.BillDate.Date) throw new UserFriendlyException("The due date cannot be before the bill date.");
        var paid = await PaidAsync(id);
        if (Math.Round(input.Amount, 2) + Eps < paid)
            throw new UserFriendlyException($"{paid:N2} has already been paid, so the bill cannot be less than that.");
        var b = await _bills.GetAsync(id);
        ApplyBill(b, input);
        await _bills.UpdateAsync(b, autoSave: true);
        return await BillDtoAsync(id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteBillAsync(int id)
    {
        if (await PaidAsync(id) > Eps)
            throw new UserFriendlyException("This bill has payments. Delete the payments first, so the money movements stay correct.");
        await _bills.DeleteAsync(id, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task<FinanceBillDto> CreateBillPaymentAsync(int id, PayFinanceBillDto input)
    {
        var bill = await _bills.GetAsync(id);
        await EnsureAccountAsync(input.AccountId);
        var amount = Math.Round(input.Amount, 2);
        var remaining = bill.Amount - await PaidAsync(id);
        if (remaining <= Eps) throw new UserFriendlyException("This bill is already fully paid.");
        if (amount > remaining + Eps) throw new UserFriendlyException($"The amount is more than what is still owed ({remaining:N2}).");

        await _txs.InsertAsync(new FinanceTransaction
        {
            Date = (input.Date ?? Clock.Now).Date, AccountId = input.AccountId, Direction = FinanceDirection.Out, Amount = amount,
            Category = "Supplier bills", Description = string.IsNullOrWhiteSpace(bill.BillNumber) ? bill.Supplier : $"{bill.Supplier} · {bill.BillNumber}",
            Reference = Clean(input.Reference), Source = FinanceTxSource.SupplierBill, SourceId = id,
        }, autoSave: true);
        return await BillDtoAsync(id);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task<FinanceBillDto> DeleteBillPaymentAsync(int id, int paymentId)
    {
        var tx = await _txs.GetAsync(paymentId);
        if (tx.Source != FinanceTxSource.SupplierBill || tx.SourceId != id)
            throw new UserFriendlyException("That payment does not belong to this bill.");
        await _txs.DeleteAsync(tx, autoSave: true);
        return await BillDtoAsync(id);
    }

    private static void ApplyBill(FinanceSupplierBill b, CreateUpdateFinanceBillDto i)
    {
        b.Supplier = i.Supplier.Trim(); b.Description = Clean(i.Description); b.BillNumber = Clean(i.BillNumber);
        b.BillDate = i.BillDate.Date; b.DueDate = i.DueDate.Date; b.Amount = Math.Round(i.Amount, 2);
        b.CategoryId = i.CategoryId; b.ReceiptUrl = Clean(i.ReceiptUrl); b.Note = Clean(i.Note);
    }

    private async Task<decimal> PaidAsync(int billId) =>
        (await _txs.GetListAsync(t => t.Source == FinanceTxSource.SupplierBill && t.SourceId == billId && t.Direction == FinanceDirection.Out)).Sum(t => t.Amount);

    private async Task<FinanceBillDto> BillDtoAsync(int id)
    {
        var c = await _calc.LoadAsync();
        return _calc.BillRows(c).First(b => b.Id == id);
    }

    // ══ LEDGER ═══════════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task CreateTransactionAsync(CreateFinanceTransactionDto input)
    {
        await _calc.EnsureDefaultsAsync();
        await EnsureAccountAsync(input.AccountId);
        await _txs.InsertAsync(new FinanceTransaction
        {
            Date = (input.Date ?? Clock.Now).Date, AccountId = input.AccountId, Direction = input.Direction, Amount = Math.Round(input.Amount, 2),
            Category = Clean(input.Category), Description = Clean(input.Description), Reference = Clean(input.Reference), Source = FinanceTxSource.Manual,
        }, autoSave: true);
    }

    /// <summary>Moves money between two of your own accounts (e.g. cash banked). It is not income or a cost.</summary>
    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task CreateTransferAsync(CreateFinanceTransferDto input)
    {
        if (input.FromAccountId == input.ToAccountId) throw new UserFriendlyException("Choose two different accounts.");
        await _calc.EnsureDefaultsAsync();
        var from = await EnsureAccountAsync(input.FromAccountId);
        var to = await EnsureAccountAsync(input.ToAccountId);
        var date = (input.Date ?? Clock.Now).Date;
        var amount = Math.Round(input.Amount, 2);
        var note = Clean(input.Note);

        var outRow = await _txs.InsertAsync(new FinanceTransaction
        {
            Date = date, AccountId = from.Id, Direction = FinanceDirection.Out, Amount = amount, Category = "Transfer",
            Description = note ?? $"Transfer to {to.Name}", Source = FinanceTxSource.Transfer,
        }, autoSave: true);
        outRow.SourceId = outRow.Id;
        await _txs.UpdateAsync(outRow, autoSave: true);
        await _txs.InsertAsync(new FinanceTransaction
        {
            Date = date, AccountId = to.Id, Direction = FinanceDirection.In, Amount = amount, Category = "Transfer",
            Description = note ?? $"Transfer from {from.Name}", Source = FinanceTxSource.Transfer, SourceId = outRow.Id,
        }, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.Finance.Delete)]
    public async Task DeleteTransactionAsync(int id)
    {
        var tx = await _txs.GetAsync(id);
        switch (tx.Source)
        {
            case FinanceTxSource.Manual:
                await _txs.DeleteAsync(tx, autoSave: true);
                break;
            case FinanceTxSource.Transfer:
                await _txs.DeleteAsync(t => t.Source == FinanceTxSource.Transfer && t.SourceId == tx.SourceId);
                break;
            default:
                throw new UserFriendlyException("This movement was created by a bill payment, an expense or incoming money. Delete it from there instead.");
        }
    }

    // ══ SMALL HELPERS ════════════════════════════════════════════════════════

    private async Task<FinanceExpenseCategory> EnsureCategoryAsync(int id)
    {
        var cat = await _categories.FirstOrDefaultAsync(x => x.Id == id);
        if (cat == null || !cat.IsActive) throw new UserFriendlyException("Choose an active expense category.");
        return cat;
    }

    private async Task<FinanceAccount> EnsureAccountAsync(int id)
    {
        var a = await _accounts.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null || !a.IsActive) throw new UserFriendlyException("Choose an active account.");
        return a;
    }

    private async Task EnsureAccountOrNullAsync(int? id)
    {
        if (id.HasValue) await EnsureAccountAsync(id.Value);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
