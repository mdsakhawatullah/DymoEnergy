namespace DymoEnergy.Settings;

public static class DymoEnergySettings
{
    private const string Prefix = "DymoEnergy";

    /// <summary>Monthly sales target in BDT, shown on Analytics › Overview. "0" = not set.</summary>
    public const string MonthlySalesTarget = Prefix + ".Analytics.MonthlySalesTarget";
}
