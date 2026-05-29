using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class add_shippingCost_to_saleInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ShippingCost",
                table: "DymoSalesInvoices",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "DymoSalesInvoices");
        }
    }
}
