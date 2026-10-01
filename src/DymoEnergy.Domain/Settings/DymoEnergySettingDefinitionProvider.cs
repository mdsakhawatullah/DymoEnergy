using Volo.Abp.Settings;

namespace DymoEnergy.Settings;

public class DymoEnergySettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(new SettingDefinition(DymoEnergySettings.MonthlySalesTarget, "0"));
    }
}
