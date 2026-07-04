using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class ADD_Comapny_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DymoCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SendStatementTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsParentCompany = table.Column<bool>(type: "bit", nullable: false),
                    HasChildCompany = table.Column<bool>(type: "bit", nullable: false),
                    ParentCompanyId = table.Column<int>(type: "int", nullable: true),
                    DefaultShippingFee = table.Column<double>(type: "float", nullable: false),
                    FreeShippingThreshold = table.Column<double>(type: "float", nullable: true),
                    IsFlatRateShipping = table.Column<bool>(type: "bit", nullable: false),
                    ShippingCurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebhookUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSyncEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoCompanies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DymoCompanies_CompanyName",
                table: "DymoCompanies",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_DymoCompanies_ParentCompanyId",
                table: "DymoCompanies",
                column: "ParentCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DymoCompanies");
        }
    }
}
