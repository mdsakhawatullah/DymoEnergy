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
using DymoEnergy.Finance;
using DymoEnergy.Shipping;
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
using DymoEnergy.Customers;
using DymoEnergy.Stock;
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

    public DbSet<FinanceSetting>         FinanceSettings          { get; set; }
    public DbSet<FinanceAccount>         FinanceAccounts          { get; set; }
    public DbSet<FinanceTransaction>     FinanceTransactions      { get; set; }
    public DbSet<FinanceExpenseCategory> FinanceExpenseCategories { get; set; }
    public DbSet<FinanceExpense>         FinanceExpenses          { get; set; }
    public DbSet<FinanceSupplierBill>    FinanceSupplierBills     { get; set; }
    public DbSet<FinanceRecurringCost>   FinanceRecurringCosts    { get; set; }
    public DbSet<FinanceListItem>        FinanceListItems         { get; set; }

    public DbSet<CourierAccount>    CourierAccounts    { get; set; }
    public DbSet<CourierCredential> CourierCredentials { get; set; }
    public DbSet<CourierApiLog>     CourierApiLogs     { get; set; }
    public DbSet<Shipment>          Shipments          { get; set; }
    public DbSet<ShippingSetting>   ShippingSettings   { get; set; }
    public DbSet<ShippingZone>      ShippingZones      { get; set; }
    public DbSet<ShippingListItem>  ShippingListItems  { get; set; }
    public DbSet<CourierRule>       CourierRules       { get; set; }
    public DbSet<CourierPayout>     CourierPayouts     { get; set; }
    public DbSet<ShipmentEvent>     ShipmentEvents     { get; set; }

    // Stock
    public DbSet<Warehouse>            Warehouses            { get; set; }
    public DbSet<StockSupplier>        StockSuppliers        { get; set; }
    public DbSet<StockBalance>         StockBalances         { get; set; }
    public DbSet<StockEntry>           StockEntries          { get; set; }
    public DbSet<StockEntryLine>       StockEntryLines       { get; set; }
    public DbSet<StockEntryAttachment> StockEntryAttachments { get; set; }
    public DbSet<StockSerial>          StockSerials          { get; set; }
    public DbSet<StockLedgerLine>      StockLedgerLines      { get; set; }
    public DbSet<StockLedgerReview>    StockLedgerReviews    { get; set; }
    public DbSet<StockLedgerCheck>     StockLedgerChecks     { get; set; }
    public DbSet<StockLedgerSetting>   StockLedgerSettings   { get; set; }

    public DbSet<Customer>             Customers             { get; set; }
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

        /* ── Finance ────────────────────────────────────────────────────── */
        builder.Entity<FinanceSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.AccentColor).IsRequired().HasMaxLength(FinanceConsts.MaxColor);
            b.Property(x => x.CurrencySymbol).IsRequired().HasMaxLength(8);
            b.Property(x => x.ReminderTemplate).HasMaxLength(FinanceConsts.MaxText);
        });

        builder.Entity<FinanceAccount>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceAccounts", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.ShortCode).IsRequired().HasMaxLength(8);
            b.Property(x => x.Color).IsRequired().HasMaxLength(FinanceConsts.MaxColor);
            b.Property(x => x.OpeningBalance).HasPrecision(18, 2);
            b.Property(x => x.PaymentMethods).HasMaxLength(FinanceConsts.MaxShort);
            b.HasIndex(x => x.Order);
        });

        builder.Entity<FinanceTransaction>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceTransactions", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Category).HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Description).HasMaxLength(FinanceConsts.MaxText);
            b.Property(x => x.Reference).HasMaxLength(FinanceConsts.MaxName);
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => new { x.Source, x.SourceId });
        });

        builder.Entity<FinanceExpenseCategory>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceExpenseCategories", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Color).IsRequired().HasMaxLength(FinanceConsts.MaxColor);
            b.Property(x => x.PlLine).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.HasIndex(x => x.Order);
        });

        builder.Entity<FinanceExpense>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceExpenses", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Description).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.PaidByNote).HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.ReceiptUrl).HasMaxLength(FinanceConsts.MaxUrl);
            b.HasIndex(x => x.Date);
            b.HasIndex(x => x.CategoryId);
        });

        builder.Entity<FinanceSupplierBill>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceSupplierBills", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Supplier).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Description).HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.BillNumber).HasMaxLength(FinanceConsts.MaxShort);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.ReceiptUrl).HasMaxLength(FinanceConsts.MaxUrl);
            b.Property(x => x.Note).HasMaxLength(FinanceConsts.MaxText);
            b.HasIndex(x => x.DueDate);
        });

        builder.Entity<FinanceRecurringCost>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceRecurringCosts", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Detail).HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Amount).HasPrecision(18, 2);
        });

        builder.Entity<FinanceListItem>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "FinanceListItems", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Title).IsRequired().HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Detail).HasMaxLength(FinanceConsts.MaxText);
            b.Property(x => x.Extra).HasMaxLength(FinanceConsts.MaxName);
            b.Property(x => x.Color).HasMaxLength(FinanceConsts.MaxColor);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.HasIndex(x => new { x.Kind, x.Order });
        });

        /* ── Shipping & couriers ────────────────────────────────────────── */
        builder.Entity<CourierAccount>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CourierAccounts", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.ShortCode).IsRequired().HasMaxLength(8);
            b.Property(x => x.Color).IsRequired().HasMaxLength(16);
            b.Property(x => x.PickupStoreId).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.PickupStoreName).HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.DefaultWeightKg).HasPrecision(9, 2);
            b.Property(x => x.LastWebhookNote).HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.CodFeePercent).HasPrecision(6, 3);
            b.Property(x => x.PayoutSchedule).HasMaxLength(ShippingConsts.MaxShort);
        });

        builder.Entity<CourierCredential>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CourierCredentials", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Key).IsRequired().HasMaxLength(64);
            b.Property(x => x.EncryptedValue).IsRequired().HasMaxLength(ShippingConsts.MaxEncrypted);
            b.HasIndex(x => new { x.CourierAccountId, x.Environment, x.Key }).IsUnique();
        });

        builder.Entity<CourierApiLog>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CourierApiLogs", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Action).IsRequired().HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.Method).HasMaxLength(16);
            b.Property(x => x.Endpoint).HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.Result).HasMaxLength(ShippingConsts.MaxName);
            b.HasIndex(x => new { x.CourierAccountId, x.Time });
        });

        builder.Entity<Shipment>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Shipments", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.ConsignmentId).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.MerchantOrderId).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.Status).IsRequired().HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.StatusSlug).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.DeliveryFee).HasPrecision(18, 2);
            b.Property(x => x.CodAmount).HasPrecision(18, 2);
            b.Property(x => x.WeightKg).HasPrecision(9, 2);
            b.Property(x => x.RecipientName).IsRequired().HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.RecipientPhone).IsRequired().HasMaxLength(32);
            b.Property(x => x.RecipientAddress).IsRequired().HasMaxLength(ShippingConsts.MaxText);
            b.Property(x => x.Note).HasMaxLength(ShippingConsts.MaxText);
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.ConsignmentId);
            b.HasIndex(x => x.PayoutId);
        });

        builder.Entity<ShippingSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ShippingSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.FreeDeliveryOver).HasPrecision(18, 2);
            b.Property(x => x.WeightIncludedKg).HasPrecision(9, 2);
            b.Property(x => x.CodFeePercent).HasPrecision(6, 3);
            b.Property(x => x.OwnTruckPerKm).HasPrecision(18, 2);
            b.Property(x => x.MinTripCharge).HasPrecision(18, 2);
        });

        builder.Entity<ShippingZone>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ShippingZones", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.Note).HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.Days).HasMaxLength(64);
            b.Property(x => x.Charge).HasPrecision(18, 2);
            b.Property(x => x.PerExtraKg).HasPrecision(18, 2);
            b.Property(x => x.CourierCost).HasPrecision(18, 2);
            b.Property(x => x.CourierPerExtraKg).HasPrecision(18, 2);
            b.Property(x => x.PathaoCityName).HasMaxLength(128);
            b.Property(x => x.PathaoZoneName).HasMaxLength(128);
            b.Property(x => x.PriceError).HasMaxLength(512);
        });

        builder.Entity<ShippingListItem>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ShippingListItems", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Title).IsRequired().HasMaxLength(ShippingConsts.MaxName);
            b.Property(x => x.Detail).HasMaxLength(ShippingConsts.MaxText);
            b.Property(x => x.Extra).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.Color).HasMaxLength(16);
            b.HasIndex(x => new { x.Kind, x.Order });
        });

        builder.Entity<CourierRule>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CourierRules", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.ProductKeyword).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.AddressContains).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.AnyItemOverKg).HasPrecision(9, 2);
            b.Property(x => x.TotalWeightUnderKg).HasPrecision(9, 2);
            b.Property(x => x.CodOver).HasPrecision(18, 2);
            b.Property(x => x.ThenNote).HasMaxLength(ShippingConsts.MaxName);
        });

        builder.Entity<CourierPayout>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "CourierPayouts", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Expected).HasPrecision(18, 2);
            b.Property(x => x.Reference).HasMaxLength(ShippingConsts.MaxShort);
            b.Property(x => x.Note).HasMaxLength(ShippingConsts.MaxText);
            b.HasIndex(x => x.CourierAccountId);
        });

        builder.Entity<ShipmentEvent>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "ShipmentEvents", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Status).IsRequired().HasMaxLength(128);
            b.Property(x => x.Source).IsRequired().HasMaxLength(16);
            b.Property(x => x.Event).HasMaxLength(64);
            b.Property(x => x.Note).HasMaxLength(512);
            b.Property(x => x.CollectedAmount).HasPrecision(18, 2);
            b.HasIndex(x => x.ShipmentId);
        });

        /* ── Stock ──────────────────────────────────────────────────────── */
        builder.Entity<Warehouse>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Warehouses", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.ShortCode).IsRequired().HasMaxLength(8);
            b.Property(x => x.Address).HasMaxLength(512);
        });

        builder.Entity<StockSupplier>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockSuppliers", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.Address).HasMaxLength(512);
            b.Property(x => x.Note).HasMaxLength(2000);
        });

        builder.Entity<StockBalance>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockBalances", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.AvgCost).HasPrecision(18, 2);
            b.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
        });

        builder.Entity<StockEntry>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockEntries", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Number).IsRequired().HasMaxLength(32);
            b.Property(x => x.InvoiceNumber).HasMaxLength(128);
            b.Property(x => x.PurchaseOrder).HasMaxLength(128);
            b.Property(x => x.TransportCost).HasPrecision(18, 2);
            b.Property(x => x.Reference).HasMaxLength(256);
            b.Property(x => x.Note).HasMaxLength(4000);
            b.Property(x => x.PostedByName).HasMaxLength(256);
            b.HasIndex(x => x.Number);
            b.HasIndex(x => new { x.Status, x.Date });
        });

        builder.Entity<StockEntryLine>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockEntryLines", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.UnitCost).HasPrecision(18, 2);
            b.Property(x => x.LandedUnitCost).HasPrecision(18, 2);
            b.HasIndex(x => x.StockEntryId);
            b.HasIndex(x => x.ProductId);
        });

        builder.Entity<StockEntryAttachment>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockEntryAttachments", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            b.Property(x => x.Url).IsRequired().HasMaxLength(1024);
            b.HasIndex(x => x.StockEntryId);
        });

        builder.Entity<StockSerial>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockSerials", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Serial).IsRequired().HasMaxLength(StockConsts.SerialMaxLength);
            b.HasIndex(x => new { x.ProductId, x.Serial }).IsUnique();
            b.HasIndex(x => x.Serial);
        });

        /* ── Stock ledger ───────────────────────────────────────────────── */
        builder.Entity<StockLedgerLine>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockLedgerLines", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.ProductName).IsRequired().HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.Sku).HasMaxLength(LedgerConsts.MaxShort);
            b.Property(x => x.WarehouseName).IsRequired().HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.UnitCost).HasPrecision(18, 2);
            b.Property(x => x.ValueBefore).HasPrecision(18, 2);
            b.Property(x => x.ValueAfter).HasPrecision(18, 2);
            b.Property(x => x.TimeZone).HasMaxLength(64);
            b.Property(x => x.UserName).IsRequired().HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.UserEmail).HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.UserRole).HasMaxLength(LedgerConsts.MaxShort);
            b.Property(x => x.ApprovedByName).HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.Property(x => x.Device).HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.SessionId).HasMaxLength(64);
            b.Property(x => x.CameFrom).HasMaxLength(LedgerConsts.MaxShort);
            b.Property(x => x.Reason).HasMaxLength(LedgerConsts.MaxReason);
            b.Property(x => x.DocumentType).HasMaxLength(64);
            b.Property(x => x.DocumentNumber).HasMaxLength(LedgerConsts.MaxShort);
            b.Property(x => x.Hash).IsRequired().HasMaxLength(LedgerConsts.HashLength);
            b.Property(x => x.PreviousHash).IsRequired().HasMaxLength(LedgerConsts.HashLength);
            b.Property(x => x.ServerName).HasMaxLength(64);
            b.Property(x => x.RequestId).HasMaxLength(64);
            b.HasIndex(x => x.Time);
            b.HasIndex(x => new { x.ProductId, x.Id });
            b.HasIndex(x => x.Flags);
            b.HasIndex(x => x.StockEntryId);
        });

        builder.Entity<StockLedgerReview>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockLedgerReviews", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.UserName).IsRequired().HasMaxLength(LedgerConsts.MaxName);
            b.Property(x => x.Action).IsRequired().HasMaxLength(16);
            b.Property(x => x.Note).HasMaxLength(LedgerConsts.MaxReason);
            b.HasIndex(x => x.LineId);
        });

        builder.Entity<StockLedgerCheck>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockLedgerChecks", DymoEnergyConsts.DbSchema);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.StartedBy).IsRequired().HasMaxLength(16);
            b.Property(x => x.UserName).HasMaxLength(LedgerConsts.MaxName);
            b.HasIndex(x => x.Time);
        });

        builder.Entity<StockLedgerSetting>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "StockLedgerSettings", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.LargeValueOver).HasPrecision(18, 2);
            b.Property(x => x.AllowedNetworks).HasMaxLength(512);
            b.Property(x => x.LastExportBy).HasMaxLength(LedgerConsts.MaxName);
        });

        /* ── Customers ──────────────────────────────────────────────────── */
        builder.Entity<Customer>(b =>
        {
            b.ToTable(DymoEnergyConsts.DbTablePrefix + "Customers", DymoEnergyConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Name).IsRequired().HasMaxLength(CustomerConsts.MaxName);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.PhoneKey).HasMaxLength(16);
            b.Property(x => x.Email).HasMaxLength(CustomerConsts.MaxName);
            b.Property(x => x.CompanyName).HasMaxLength(CustomerConsts.MaxName);
            b.Property(x => x.TaxId).HasMaxLength(64);
            b.Property(x => x.Address).HasMaxLength(512);
            b.Property(x => x.Area).HasMaxLength(CustomerConsts.MaxShort);
            b.Property(x => x.City).HasMaxLength(CustomerConsts.MaxShort);
            b.Property(x => x.District).HasMaxLength(CustomerConsts.MaxShort);
            b.Property(x => x.AssignedTo).HasMaxLength(CustomerConsts.MaxName);
            b.Property(x => x.Note).HasMaxLength(CustomerConsts.MaxText);
            b.Property(x => x.Tags).HasMaxLength(512);
            b.Property(x => x.CreditLimit).HasPrecision(18, 2);
            b.HasIndex(x => x.PhoneKey);
            b.HasIndex(x => x.Name);
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
