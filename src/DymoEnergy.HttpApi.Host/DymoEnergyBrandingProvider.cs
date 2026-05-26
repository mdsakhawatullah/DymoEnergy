using Microsoft.Extensions.Localization;
using DymoEnergy.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace DymoEnergy;

[Dependency(ReplaceServices = true)]
public class DymoEnergyBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<DymoEnergyResource> _localizer;

    public DymoEnergyBrandingProvider(IStringLocalizer<DymoEnergyResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
