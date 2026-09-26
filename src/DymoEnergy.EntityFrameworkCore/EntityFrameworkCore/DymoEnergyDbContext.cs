using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using DymoEnergy.Books;
using DymoEnergy.AdminSiteSettings;
using DymoEnergy.Catalogues;
using DymoEnergy.Products;
using DymoEnergy.SalesInvoices;
using DymoEnergy.Orders;
using DymoEnergy.Companies;
using DymoEnergy.QuoteRequests;
using DymoEnergy.UserSiteSettings;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;

namespace DymoEnergy.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ConnectionStringName("Default")]
public class DymoEnergyDbContext :
    AbpDbContext<DymoEnergyDbContext>,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    public DbSet<Book> Books { get; set; }
    public DbSet<AdminSiteSetting> AdminSiteSettings { get; set; }
    public DbSet<Catalogue> Catalogues { get; set; }
    public DbSet<CatalogueImage> CatalogueImages { get; set; }
    public DbSet<Product> Products{ get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<SalesInvoice>     SalesInvoices     { get; set; }
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }
    public DbSet<Order>     Orders     { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Company>   Companies  { get; set; }
    public DbSet<QuoteRequest> QuoteRequests { get; set; }
    public DbSet<UserSiteSetting>      UserSiteSettings      { get; set; }
    public DbSet<UserSiteSettingImage> UserSiteSettingImages { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext 
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext .
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    #endregion

    public DymoEnergyDbContext(DbContextOptions<DymoEnergyDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureBlobStoring();
        
        builder.Entity<Book>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Books",
                DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });
        
        builder.Entity<AdminSiteSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "AdminSiteSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
        });

        /* ── Catalogues ──────────────────────────────────────────────── */
        builder.Entity<Catalogue>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Catalogues", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();   // auto-increment int PK
        });

        builder.Entity<CatalogueImage>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CatalogueImages", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Id).ValueGeneratedOnAdd();   // auto-increment int PK
        });

        /* ── Products ────────────────────────────────────────────────── */
        builder.Entity<Product>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Products", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<ProductImage>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProductImages", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<ProductReview>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProductReviews", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        /* ── Sales Invoices ──────────────────────────────────────────────── */
        builder.Entity<SalesInvoice>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "SalesInvoices", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.InvoiceNumber).IsUnique();
        });

        builder.Entity<SalesInvoiceItem>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "SalesInvoiceItems", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.InvoiceId);
        });

        /* ── Orders ─────────────────────────────────────────────────────── */
        builder.Entity<Order>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Orders", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);
        });

        builder.Entity<OrderItem>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "OrderItems", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.OrderId);
        });

        /* ── Companies ──────────────────────────────────────────────────── */
        builder.Entity<Company>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Companies", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.CompanyName);
            b.HasIndex(x => x.ParentCompanyId);
        });

        /* ── Quote Requests ─────────────────────────────────────────────── */
        builder.Entity<QuoteRequest>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "QuoteRequests", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(QuoteRequestConsts.MaxNameLength);
            b.Property(x => x.Phone).HasMaxLength(QuoteRequestConsts.MaxPhoneLength);
            b.Property(x => x.Email).HasMaxLength(QuoteRequestConsts.MaxEmailLength);
            b.Property(x => x.Interest).HasMaxLength(QuoteRequestConsts.MaxInterestLength);
            b.Property(x => x.Message).HasMaxLength(QuoteRequestConsts.MaxMessageLength);
            b.HasIndex(x => x.Status);
        });

        /* ── User Site Settings ──────────────────────────────────────────── */
        builder.Entity<UserSiteSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "UserSiteSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<UserSiteSettingImage>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "UserSiteSettingImages", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.UserSiteSettingId);
        });
    }
}
