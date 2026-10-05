using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_Product_Specifications_Download : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specifications",
                table: "DymoProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Download",
                table: "DymoProducts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "DymoProducts");

            migrationBuilder.DropColumn(
                name: "Download",
                table: "DymoProducts");
        }
    }
}