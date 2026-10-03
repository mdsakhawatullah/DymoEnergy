using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace DymoEnergy.Shipping;

public interface IShippingAppService : IApplicationService
{
    Task<ShippingOverviewDto> GetOverviewAsync();

    // ── Couriers & keys ───────────────────────────────────────────────────
    Task<CourierDetailDto>    GetCourierAsync(int id);
    Task<CourierDetailDto>    CreateCourierAsync(CreateCourierDto input);
    Task<CourierDetailDto>    UpdateCourierSettingsAsync(int id, UpdateCourierSettingsDto input);
    Task<CourierDetailDto>    UpdateCourierCredentialAsync(int id, UpdateCourierCredentialDto input);
    Task<string>              GetCourierCredentialRevealAsync(int id, CourierCredentialKeyDto input);
    Task<CourierDetailDto>    UpdateCourierEnvironmentAsync(int id, SetCourierEnvironmentDto input);
    Task<CourierTestResultDto> TestCourierAsync(int id);
    Task<CourierTestResultDto> RefreshCourierTokenAsync(int id);
    Task<CourierDetailDto>    DisconnectCourierAsync(int id);
    Task<List<CourierTestResultDto>> TestAllCouriersAsync();

    // ── Pathao lookups ────────────────────────────────────────────────────
    Task<List<PathaoStoreDto>>    GetPathaoStoresAsync(int id);
    Task<string>                  CreatePathaoStoreAsync(int id, CreatePathaoStoreDto input);
    Task<List<PathaoLocationDto>> GetPathaoCitiesAsync(int id);
    Task<List<PathaoLocationDto>> GetPathaoZonesAsync(int id, int cityId);
    Task<List<PathaoLocationDto>> GetPathaoAreasAsync(int id, int zoneId);
    Task<PathaoPriceDto>          GetPathaoPriceAsync(int id, PathaoPriceInputDto input);

    // ── Shipments ─────────────────────────────────────────────────────────
    Task<ShipmentsPageDto>        GetShipmentsAsync(GetShipmentsInput input);
    Task<List<SendParcelResultDto>> SendParcelsAsync(SendParcelsDto input);
    Task<ShipmentDto>             RefreshShipmentAsync(int id);
}

/// <summary>Handles status updates pushed by a courier. Called by an anonymous controller.</summary>
public interface IShippingWebhookService : ITransientDependency
{
    /// <returns>HTTP status to answer with, plus any headers the courier expects back.</returns>
    Task<(int StatusCode, Dictionary<string, string> Headers)> HandleAsync(int courierAccountId, IDictionary<string, string> headers, string body);
}
