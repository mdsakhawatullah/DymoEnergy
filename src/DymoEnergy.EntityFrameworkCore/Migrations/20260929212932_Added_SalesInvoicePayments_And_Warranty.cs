using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_SalesInvoicePayments_And_Warranty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AdditionalDiscount",
                table: "DymoSalesInvoices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "DymoSalesInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountNote",
                table: "DymoSalesInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxInclusive",
                table: "DymoSalesInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumbers",
                table: "DymoSalesInvoiceItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "DymoSalesInvoiceItems",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DymoSalesInvoicePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    PaidOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoSalesInvoicePayments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DymoSalesInvoicePayments_InvoiceId",
                table: "DymoSalesInvoicePayments",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DymoSalesInvoicePayments");

            migrationBuilder.DropColumn(
                name: "AdditionalDiscount",
                table: "DymoSalesInvoices");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "DymoSalesInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountNote",
                table: "DymoSalesInvoices");

            migrationBuilder.DropColumn(
                name: "TaxInclusive",
                table: "DymoSalesInvoices");

            migrationBuilder.DropColumn(
                name: "SerialNumbers",
                table: "DymoSalesInvoiceItems");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "DymoSalesInvoiceItems");
        }
    }
}
