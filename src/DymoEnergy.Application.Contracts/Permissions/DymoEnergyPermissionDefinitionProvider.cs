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

        var productsPermission = myGroup.AddPermission(DymoEnergyPermissions.Products.Default, L("Permission:Products"));
        productsPermission.AddChild(DymoEnergyPermissions.Products.Create, L("Permission:Products.Create"));
        productsPermission.AddChild(DymoEnergyPermissions.Products.Edit,   L("Permission:Products.Edit"));
        productsPermission.AddChild(DymoEnergyPermissions.Products.Delete, L("Permission:Products.Delete"));

        var salesInvoicesPermission = myGroup.AddPermission(DymoEnergyPermissions.SalesInvoices.Default, L("Permission:SalesInvoices"));
        salesInvoicesPermission.AddChild(DymoEnergyPermissions.SalesInvoices.Create, L("Permission:SalesInvoices.Create"));
        salesInvoicesPermission.AddChild(DymoEnergyPermissions.SalesInvoices.Edit,   L("Permission:SalesInvoices.Edit"));
        salesInvoicesPermission.AddChild(DymoEnergyPermissions.SalesInvoices.Delete, L("Permission:SalesInvoices.Delete"));

        var userSiteSettingsPermission = myGroup.AddPermission(DymoEnergyPermissions.UserSiteSettings.Default, L("Permission:UserSiteSettings"));
        userSiteSettingsPermission.AddChild(DymoEnergyPermissions.UserSiteSettings.Create, L("Permission:UserSiteSettings.Create"));
        userSiteSettingsPermission.AddChild(DymoEnergyPermissions.UserSiteSettings.Edit,   L("Permission:UserSiteSettings.Edit"));
        userSiteSettingsPermission.AddChild(DymoEnergyPermissions.UserSiteSettings.Delete, L("Permission:UserSiteSettings.Delete"));

        var ordersPermission = myGroup.AddPermission(DymoEnergyPermissions.Orders.Default, L("Permission:Orders"));
        ordersPermission.AddChild(DymoEnergyPermissions.Orders.Create, L("Permission:Orders.Create"));
        ordersPermission.AddChild(DymoEnergyPermissions.Orders.Edit,   L("Permission:Orders.Edit"));
        ordersPermission.AddChild(DymoEnergyPermissions.Orders.Delete, L("Permission:Orders.Delete"));

        var companiesPermission = myGroup.AddPermission(DymoEnergyPermissions.Companies.Default, L("Permission:Companies"));
        companiesPermission.AddChild(DymoEnergyPermissions.Companies.Create, L("Permission:Companies.Create"));
        companiesPermission.AddChild(DymoEnergyPermissions.Companies.Edit,   L("Permission:Companies.Edit"));
        companiesPermission.AddChild(DymoEnergyPermissions.Companies.Delete, L("Permission:Companies.Delete"));

        var quoteRequestsPermission = myGroup.AddPermission(DymoEnergyPermissions.QuoteRequests.Default, L("Permission:QuoteRequests"));
        quoteRequestsPermission.AddChild(DymoEnergyPermissions.QuoteRequests.Edit,   L("Permission:QuoteRequests.Edit"));
        quoteRequestsPermission.AddChild(DymoEnergyPermissions.QuoteRequests.Delete, L("Permission:QuoteRequests.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<DymoEnergyResource>(name);
    }
}
