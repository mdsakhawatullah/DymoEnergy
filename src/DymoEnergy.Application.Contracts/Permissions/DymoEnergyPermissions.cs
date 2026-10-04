namespace DymoEnergy.Permissions;

public static class DymoEnergyPermissions
{
    public const string GroupName = "DymoEnergy";


    public static class Books
    {
        public const string Default = GroupName + ".Books";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
    
    public static class AdminSiteSettings
    {
        public const string Default = GroupName + ".AdminSiteSettings";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class Categories
    {
        public const string Default = GroupName + ".Categories";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class Products
    {
        public const string Default = GroupName + ".Products";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class SalesInvoices
    {
        public const string Default = GroupName + ".SalesInvoices";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class UserSiteSettings
    {
        public const string Default = GroupName + ".UserSiteSettings";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class Orders
    {
        public const string Default = GroupName + ".Orders";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class ProjectPlanning
    {
        public const string Default = GroupName + ".ProjectPlanning";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class Compliance
    {
        public const string Default = GroupName + ".Compliance";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class Finance
    {
        public const string Default = GroupName + ".Finance";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class Shipping
    {
        public const string Default    = GroupName + ".Shipping";
        /// <summary>Send parcels to a courier (creates real consignments).</summary>
        public const string Send       = Default + ".Send";
        public const string Edit       = Default + ".Edit";
        /// <summary>See, change and reveal courier API keys. Give this to as few people as possible.</summary>
        public const string ManageKeys = Default + ".ManageKeys";
    }

    public static class Stock
    {
        public const string Default = GroupName + ".Stock";
        /// <summary>Create and change draft entries, warehouses and suppliers.</summary>
        public const string Edit    = Default + ".Edit";
        /// <summary>Post entries (changes product stock) and reverse posted ones.</summary>
        public const string Post    = Default + ".Post";
    }

    public static class Companies
    {
        public const string Default = GroupName + ".Companies";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }

    public static class QuoteRequests
    {
        public const string Default = GroupName + ".QuoteRequests";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }
}
