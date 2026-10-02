using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using DymoEnergy.Books;
using DymoEnergy.AdminSiteSettings;
using DymoEnergy.Categories;
using DymoEnergy.Products;
using DymoEnergy.SalesInvoices;
using DymoEnergy.Orders;
using DymoEnergy.Companies;
using DymoEnergy.QuoteRequests;
using DymoEnergy.ProjectPlanning;
using DymoEnergy.Compliance;
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
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryImage> CategoryImages { get; set; }
    public DbSet<Product> Products{ get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<SalesInvoice>     SalesInvoices     { get; set; }
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }
    public DbSet<SalesInvoicePayment> SalesInvoicePayments { get; set; }
    public DbSet<Order>     Orders     { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Company>   Companies  { get; set; }
    public DbSet<QuoteRequest> QuoteRequests { get; set; }

    public DbSet<ProjectStage>           ProjectStages           { get; set; }
    public DbSet<ProjectTeam>            ProjectTeams            { get; set; }
    public DbSet<Project>                Projects                { get; set; }
    public DbSet<ProjectScheduleEntry>   ProjectScheduleEntries  { get; set; }
    public DbSet<ProjectStageHistory>    ProjectStageHistories   { get; set; }
    public DbSet<ProjectSeasonNote>      ProjectSeasonNotes      { get; set; }
    public DbSet<ProjectPlanningSetting> ProjectPlanningSettings { get; set; }

    public DbSet<ComplianceSetting>         ComplianceSettings         { get; set; }
    public DbSet<ComplianceLicence>         ComplianceLicences         { get; set; }
    public DbSet<ComplianceFiling>          ComplianceFilings          { get; set; }
    public DbSet<ComplianceCertificate>     ComplianceCertificates     { get; set; }
    public DbSet<ComplianceListItem>        ComplianceListItems        { get; set; }
    public DbSet<ComplianceProjectDocument> ComplianceProjectDocuments { get; set; }
    public DbSet<ComplianceFile>            ComplianceFiles            { get; set; }
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

        /* ── Categories ──────────────────────────────────────────────── */
        builder.Entity<Category>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Categories", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();   // auto-increment int PK
        });

        builder.Entity<CategoryImage>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CategoryImages", DymoEnergyConsts.DbSchema);
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

        builder.Entity<SalesInvoicePayment>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "SalesInvoicePayments", DymoEnergyConsts.DbSchema);
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

        /* ── Project planning ───────────────────────────────────────────── */
        builder.Entity<ProjectStage>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProjectStages", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.Color).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxColorLength);
            b.Property(x => x.Responsible).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.DurationText).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.ProducesText).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.ChecklistText).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.HasIndex(x => x.Order);
        });

        builder.Entity<ProjectTeam>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProjectTeams", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.Description).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.Color).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxColorLength);
            b.HasIndex(x => x.Order);
        });

        builder.Entity<Project>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Projects", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Code).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.Title).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.District).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.Tags).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.StatusNote).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.Notes).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.Property(x => x.BlockedReason).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.WaitingFor).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.ActionLabel).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.MaterialNote).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.ChecklistDone).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.HasIndex(x => x.StageId);
            b.HasIndex(x => x.TeamId);
        });

        builder.Entity<ProjectScheduleEntry>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProjectScheduleEntries", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Note).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.ProjectId);
            b.HasIndex(x => x.TeamId);
        });

        builder.Entity<ProjectStageHistory>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProjectStageHistories", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.ProjectId);
            b.HasIndex(x => x.StageId);
        });

        builder.Entity<ProjectSeasonNote>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProjectSeasonNotes", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Title).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.Period).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.Description).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.Property(x => x.Color).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxColorLength);
        });

        builder.Entity<ProjectPlanningSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ProjectPlanningSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.AccentColor).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxColorLength);
            b.Property(x => x.CurrencySymbol).IsRequired().HasMaxLength(8);
            b.Property(x => x.PageTitle).IsRequired().HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.PageSubtitle).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.Property(x => x.NewProjectLabel).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.BoardTabLabel).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.WeekTabLabel).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.AttentionTabLabel).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.StagesTabLabel).HasMaxLength(ProjectPlanningConsts.MaxNameLength);
            b.Property(x => x.StagesIntro).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.Property(x => x.AttentionTitle).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.ScheduleTitle).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.TimeTitle).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.TimeFootnote).HasMaxLength(ProjectPlanningConsts.MaxLongText);
            b.Property(x => x.SeasonTitle).HasMaxLength(ProjectPlanningConsts.MaxShortText);
            b.Property(x => x.SeasonSubtitle).HasMaxLength(ProjectPlanningConsts.MaxLongText);
        });

        /* ── Compliance & documents ─────────────────────────────────────── */
        builder.Entity<ComplianceSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.AccentColor).IsRequired().HasMaxLength(ComplianceConsts.MaxColor);
            b.Property(x => x.ImportRequiredDocs).HasMaxLength(ComplianceConsts.MaxText);
        });

        builder.Entity<ComplianceLicence>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceLicences", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Description).HasMaxLength(ComplianceConsts.MaxText);
            b.Property(x => x.Number).HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.IssuedBy).HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Owner).HasMaxLength(ComplianceConsts.MaxName);
            b.HasIndex(x => x.Order);
        });

        builder.Entity<ComplianceFiling>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceFilings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Title).IsRequired().HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Detail).HasMaxLength(ComplianceConsts.MaxText);
            b.Property(x => x.Owner).HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.ActionLabel).HasMaxLength(ComplianceConsts.MaxShort);
            b.HasIndex(x => x.DueDate);
        });

        builder.Entity<ComplianceCertificate>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceCertificates", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Category).HasMaxLength(ComplianceConsts.MaxShort);
            b.Property(x => x.Supplier).HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Badges).HasMaxLength(ComplianceConsts.MaxText);
            b.Property(x => x.TestReport).HasMaxLength(ComplianceConsts.MaxName);
        });

        builder.Entity<ComplianceListItem>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceListItems", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Title).IsRequired().HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Description).HasMaxLength(ComplianceConsts.MaxText);
            b.Property(x => x.Extra).HasMaxLength(ComplianceConsts.MaxText);
            b.Property(x => x.Color).HasMaxLength(ComplianceConsts.MaxColor);
            b.HasIndex(x => new { x.Kind, x.Order });
        });

        builder.Entity<ComplianceProjectDocument>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceProjectDocuments", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => new { x.ProjectId, x.DocTypeId }).IsUnique();
        });

        builder.Entity<ComplianceFile>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ComplianceFiles", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.FileName).IsRequired().HasMaxLength(ComplianceConsts.MaxName);
            b.Property(x => x.Url).IsRequired().HasMaxLength(ComplianceConsts.MaxUrl);
            b.HasIndex(x => new { x.OwnerKind, x.OwnerId });
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
            b.Property(x => x.EstimatedSize).HasMaxLength(QuoteRequestConsts.MaxDetailLength);
            b.Property(x => x.MonthlyBill).HasMaxLength(QuoteRequestConsts.MaxDetailLength);
            b.Property(x => x.RoofSite).HasMaxLength(QuoteRequestConsts.MaxDetailLength);
            b.Property(x => x.Location).HasMaxLength(QuoteRequestConsts.MaxDetailLength);
            b.Property(x => x.AdminNote).HasMaxLength(QuoteRequestConsts.MaxAdminNoteLength);
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
