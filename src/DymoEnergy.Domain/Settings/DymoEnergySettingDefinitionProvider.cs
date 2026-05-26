using Volo.Abp.Settings;

namespace DymoEnergy.Settings;

public class DymoEnergySettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(DymoEnergySettings.MySetting1));
    }
}
