using DymoEnergy.Localization;
using Volo.Abp.Application.Services;

namespace DymoEnergy;

/* Inherit your application services from this class.
 */
public abstract class DymoEnergyAppService : ApplicationService
{
    protected DymoEnergyAppService()
    {
        LocalizationResource = typeof(DymoEnergyResource);
    }
}
