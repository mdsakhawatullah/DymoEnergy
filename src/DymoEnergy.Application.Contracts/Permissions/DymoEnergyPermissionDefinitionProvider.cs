using DymoEnergy.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace DymoEnergy.Permissions;

public class DymoEnergyPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(DymoEnergyPermissions.GroupName);

        var booksPermission = myGroup.AddPermission(DymoEnergyPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(DymoEnergyPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(DymoEnergyPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(DymoEnergyPermissions.Books.Delete, L("Permission:Books.Delete"));
        var adminSiteSettingsPermission = myGroup.AddPermission(DymoEnergyPermissions.AdminSiteSettings.Default, L("Permission:AdminSiteSettings"));
        adminSiteSettingsPermission.AddChild(DymoEnergyPermissions.AdminSiteSettings.Create, L("Permission:AdminSiteSettings.Create"));
        adminSiteSettingsPermission.AddChild(DymoEnergyPermissions.AdminSiteSettings.Edit,   L("Permission:AdminSiteSettings.Edit"));
        adminSiteSettingsPermission.AddChild(DymoEnergyPermissions.AdminSiteSettings.Delete, L("Permission:AdminSiteSettings.Delete"));

        var cataloguesPermission = myGroup.AddPermission(DymoEnergyPermissions.Catalogues.Default, L("Permission:Catalogues"));
        cataloguesPermission.AddChild(DymoEnergyPermissions.Catalogues.Create, L("Permission:Catalogues.Create"));
        cataloguesPermission.AddChild(DymoEnergyPermissions.Catalogues.Edit,   L("Permission:Catalogues.Edit"));
        cataloguesPermission.AddChild(DymoEnergyPermissions.Catalogues.Delete, L("Permission:Catalogues.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<DymoEnergyResource>(name);
    }
}
