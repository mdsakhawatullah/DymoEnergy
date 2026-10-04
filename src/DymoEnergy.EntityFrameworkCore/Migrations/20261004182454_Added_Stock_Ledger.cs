using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_Stock_Ledger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DymoStockLedgerChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinesChecked = table.Column<int>(type: "integer", nullable: false),
                    Ok = table.Column<bool>(type: "boolean", nullable: false),
                    FirstBadLineId = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    StartedBy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoStockLedgerChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoStockLedgerLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Movement = table.Column<int>(type: "integer", nullable: false),
                    Change = table.Column<int>(type: "integer", nullable: false),
                    QuantityBefore = table.Column<int>(type: "integer", nullable: false),
                    QuantityAfter = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValueBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValueAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Serials = table.Column<string>(type: "text", nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UserRole = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SignedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TwoStepUsed = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Device = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CameFrom = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DocumentNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StockEntryId = table.Column<int>(type: "integer", nullable: true),
                    ReversesLineId = table.Column<int>(type: "integer", nullable: true),
                    Flags = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ServerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoStockLedgerLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoStockLedgerReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LineId = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoStockLedgerReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoStockLedgerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlagNoReason = table.Column<bool>(type: "boolean", nullable: false),
                    FlagNoPhoto = table.Column<bool>(type: "boolean", nullable: false),
                    FlagQuicklyReversed = table.Column<bool>(type: "boolean", nullable: false),
                    QuicklyReversedMinutes = table.Column<int>(type: "integer", nullable: false),
                    FlagOutsideHours = table.Column<bool>(type: "boolean", nullable: false),
                    WorkingFromHour = table.Column<int>(type: "integer", nullable: false),
                    WorkingToHour = table.Column<int>(type: "integer", nullable: false),
                    FlagBelowZero = table.Column<bool>(type: "boolean", nullable: false),
                    FlagLargeValue = table.Column<bool>(type: "boolean", nullable: false),
                    LargeValueOver = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AskPasswordAgain = table.Column<bool>(type: "boolean", nullable: false),
                    TwoPeopleForBigWriteOffs = table.Column<bool>(type: "boolean", nullable: false),
                    RequireTwoStep = table.Column<bool>(type: "boolean", nullable: false),
                    OnlyFromOfficeNetworks = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedNetworks = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    EmailOwnerOnReversal = table.Column<bool>(type: "boolean", nullable: false),
                    KeepYears = table.Column<int>(type: "integer", nullable: false),
                    LastExportAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastExportBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_DymoStockLedgerSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DymoStockLedgerChecks_Time",
                table: "DymoStockLedgerChecks",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_DymoStockLedgerLines_Flags",
                table: "DymoStockLedgerLines",
                column: "Flags");

            migrationBuilder.CreateIndex(
                name: "IX_DymoStockLedgerLines_ProductId_Id",
                table: "DymoStockLedgerLines",
                columns: new[] { "ProductId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DymoStockLedgerLines_StockEntryId",
                table: "DymoStockLedgerLines",
                column: "StockEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoStockLedgerLines_Time",
                table: "DymoStockLedgerLines",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_DymoStockLedgerReviews_LineId",
                table: "DymoStockLedgerReviews",
                column: "LineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DymoStockLedgerChecks");

            migrationBuilder.DropTable(
                name: "DymoStockLedgerLines");

            migrationBuilder.DropTable(
                name: "DymoStockLedgerReviews");

            migrationBuilder.DropTable(
                name: "DymoStockLedgerSettings");
        }
    }
}
