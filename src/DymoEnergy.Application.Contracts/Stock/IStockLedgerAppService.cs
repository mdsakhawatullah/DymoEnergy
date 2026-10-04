using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Stock;

/// <summary>The stock ledger: every change to every quantity, and the proof that none of it was altered.</summary>
public interface IStockLedgerAppService : IApplicationService
{
    Task<LedgerHeaderDto>     GetHeaderAsync();

    // All movements
    Task<LedgerLinesPageDto>  GetLinesAsync(GetLedgerLinesInput input);
    Task<LedgerLineDetailDto> GetLineAsync(int id);

    // One product
    Task<List<LedgerProductOptionDto>> GetProductOptionsAsync();
    Task<LedgerProductDto>    GetProductAsync(int productId, int? days);

    // Needs a look
    Task<LedgerNeedsLookDto>  GetNeedsLookAsync();
    Task<LedgerLineDetailDto> ReviewLineAsync(int id, ReviewLedgerLineDto input);

    // Who has access
    Task<LedgerAccessDto>     GetAccessAsync();

    // Proof & keeping
    Task<LedgerProofDto>      GetProofAsync();
    /// <summary>Walks the whole chain, recomputes every seal and records the result.</summary>
    Task<LedgerCheckResultDto> VerifyChainAsync();

    Task<LedgerSettingDto>    UpdateSettingAsync(LedgerSettingDto input);
}
