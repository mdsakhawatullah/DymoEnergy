using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSiteSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppAdminSiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SecondaryColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TextColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TextMutedColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LinkColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CardBgColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NavbarBgColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NavbarTextColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SidebarBgColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SidebarTextColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SidebarActiveBgColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ButtonPrimaryBgColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ButtonPrimaryTextColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FontFamily = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FontSizeBase = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAdminSiteSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAdminSiteSettings");
        }
    }
}
