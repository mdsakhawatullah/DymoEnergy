using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Shipping;

/// <summary>Charges & zones, cash on delivery, and rules & packaging.</summary>
public interface IShippingConfigAppService : IApplicationService
{
    // ── Charges & zones ───────────────────────────────────────────────────
    Task<ChargesPageDto>     GetChargesAsync();
    Task<ShippingSettingDto> UpdateSettingAsync(ShippingSettingDto input);
    Task<ShippingZoneDto>    CreateZoneAsync(CreateUpdateShippingZoneDto input);
    Task<ShippingZoneDto>    UpdateZoneAsync(int id, CreateUpdateShippingZoneDto input);
    Task                     DeleteZoneAsync(int id);

    // ── Lists (big items, return policies, packing, customer messages) ────
    Task<ShippingItemDto> CreateItemAsync(CreateUpdateShippingItemDto input);
    Task<ShippingItemDto> UpdateItemAsync(int id, CreateUpdateShippingItemDto input);
    Task                  DeleteItemAsync(int id);

    // ── Rules & packaging ─────────────────────────────────────────────────
    Task<RulesPageDto>   GetRulesAsync();
    Task<CourierRuleDto> CreateRuleAsync(CreateUpdateCourierRuleDto input);
    Task<CourierRuleDto> UpdateRuleAsync(int id, CreateUpdateCourierRuleDto input);
    Task                 DeleteRuleAsync(int id);

    // ── Cash on delivery ──────────────────────────────────────────────────
    Task<CodPageDto>   GetCodAsync();
    Task<CodPayoutDto> CreatePayoutAsync(CreateCourierPayoutDto input);
    Task               DeletePayoutAsync(int id);
}
