using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class AddCataloguesSectionToUserSiteSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CataloguesDescription",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CataloguesTitle",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CataloguesDescription",
                table: "DymoUserSiteSettings");

            migrationBuilder.DropColumn(
                name: "CataloguesTitle",
                table: "DymoUserSiteSettings");
        }
    }
}
