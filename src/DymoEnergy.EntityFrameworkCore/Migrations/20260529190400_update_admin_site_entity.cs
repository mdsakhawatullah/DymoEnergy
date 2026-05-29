using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class update_admin_site_entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportEmail",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "DymoAdminSiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "City",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "State",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "SupportEmail",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "DymoAdminSiteSettings");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "DymoAdminSiteSettings");
        }
    }
}
