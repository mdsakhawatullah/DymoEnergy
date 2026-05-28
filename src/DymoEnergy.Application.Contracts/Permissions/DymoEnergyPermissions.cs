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

    public static class Catalogues
    {
        public const string Default = GroupName + ".Catalogues";
        public const string Create  = Default + ".Create";
        public const string Edit    = Default + ".Edit";
        public const string Delete  = Default + ".Delete";
    }
}
