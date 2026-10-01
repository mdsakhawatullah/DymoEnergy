using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_QuoteRequest_Requirement_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "DymoQuoteRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedSize",
                table: "DymoQuoteRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "DymoQuoteRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonthlyBill",
                table: "DymoQuoteRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoofSite",
                table: "DymoQuoteRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "DymoQuoteRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedSize",
                table: "DymoQuoteRequests");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "DymoQuoteRequests");

            migrationBuilder.DropColumn(
                name: "MonthlyBill",
                table: "DymoQuoteRequests");

            migrationBuilder.DropColumn(
                name: "RoofSite",
                table: "DymoQuoteRequests");
        }
    }
}
