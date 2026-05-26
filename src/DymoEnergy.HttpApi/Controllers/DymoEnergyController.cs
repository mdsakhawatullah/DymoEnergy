using DymoEnergy.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace DymoEnergy.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class DymoEnergyController : AbpControllerBase
{
    protected DymoEnergyController()
    {
        LocalizationResource = typeof(DymoEnergyResource);
    }
}
