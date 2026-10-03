using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Finance;

public interface IFinanceAppService : IApplicationService
{
    // ── Page views ────────────────────────────────────────────────────────
    Task<FinanceOverviewDto> GetOverviewAsync(FinancePeriodInput input);
    Task<FinanceCashDto>     GetCashAsync(FinancePeriodInput input);
    Task<FinanceDuesDto>     GetDuesAsync(FinancePeriodInput input);
    Task<FinanceBillsDto>    GetBillsAsync(FinancePeriodInput input);
    Task<FinanceExpensesDto> GetExpensesAsync(FinancePeriodInput input);
    Task<FinancePnlDto>      GetPnlAsync(FinancePeriodInput input);

    // ── Settings ──────────────────────────────────────────────────────────
    Task<FinanceSettingDto> UpdateSettingAsync(UpdateFinanceSettingDto input);

    // ── Accounts ──────────────────────────────────────────────────────────
    Task<FinanceAccountDto> CreateAccountAsync(CreateUpdateFinanceAccountDto input);
    Task<FinanceAccountDto> UpdateAccountAsync(int id, CreateUpdateFinanceAccountDto input);
    Task                    DeleteAccountAsync(int id);
    Task<FinanceAccountDto> UpdateAccountReconciledAsync(int id);

    // ── Expense categories ────────────────────────────────────────────────
    Task<FinanceCategoryDto> CreateCategoryAsync(CreateUpdateFinanceCategoryDto input);
    Task<FinanceCategoryDto> UpdateCategoryAsync(int id, CreateUpdateFinanceCategoryDto input);
    Task                     DeleteCategoryAsync(int id);

    // ── Recurring costs ───────────────────────────────────────────────────
    Task<FinanceRecurringDto> CreateRecurringAsync(CreateUpdateFinanceRecurringDto input);
    Task<FinanceRecurringDto> UpdateRecurringAsync(int id, CreateUpdateFinanceRecurringDto input);
    Task                      DeleteRecurringAsync(int id);
    Task<FinanceExpenseDto>   CreateRecurringPaymentAsync(int id, PayRecurringDto input);

    // ── Generic lists (in transit, advances, LCs, dealer credit, notes) ───
    Task<FinanceItemDto> CreateItemAsync(CreateUpdateFinanceItemDto input);
    Task<FinanceItemDto> UpdateItemAsync(int id, CreateUpdateFinanceItemDto input);
    Task                 DeleteItemAsync(int id);
    Task                 CreateItemReceiptAsync(int id, ReceiveInTransitDto input);

    // ── Expenses ──────────────────────────────────────────────────────────
    Task<FinanceExpenseDto> CreateExpenseAsync(CreateUpdateFinanceExpenseDto input);
    Task<FinanceExpenseDto> UpdateExpenseAsync(int id, CreateUpdateFinanceExpenseDto input);
    Task                    DeleteExpenseAsync(int id);

    // ── Supplier bills ────────────────────────────────────────────────────
    Task<FinanceBillDto> CreateBillAsync(CreateUpdateFinanceBillDto input);
    Task<FinanceBillDto> UpdateBillAsync(int id, CreateUpdateFinanceBillDto input);
    Task                 DeleteBillAsync(int id);
    Task<FinanceBillDto> CreateBillPaymentAsync(int id, PayFinanceBillDto input);
    Task<FinanceBillDto> DeleteBillPaymentAsync(int id, int paymentId);

    // ── Ledger ────────────────────────────────────────────────────────────
    Task CreateTransactionAsync(CreateFinanceTransactionDto input);
    Task CreateTransferAsync(CreateFinanceTransferDto input);
    Task DeleteTransactionAsync(int id);
}
