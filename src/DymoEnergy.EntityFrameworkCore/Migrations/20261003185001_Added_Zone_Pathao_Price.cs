using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_Zone_Pathao_Price : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CourierPerExtraKg",
                table: "DymoShippingZones",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PathaoCityId",
                table: "DymoShippingZones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PathaoCityName",
                table: "DymoShippingZones",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PathaoZoneId",
                table: "DymoShippingZones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PathaoZoneName",
                table: "DymoShippingZones",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceCheckedAt",
                table: "DymoShippingZones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceError",
                table: "DymoShippingZones",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourierPerExtraKg",
                table: "DymoShippingZones");

            migrationBuilder.DropColumn(
                name: "PathaoCityId",
                table: "DymoShippingZones");

            migrationBuilder.DropColumn(
                name: "PathaoCityName",
                table: "DymoShippingZones");

            migrationBuilder.DropColumn(
                name: "PathaoZoneId",
                table: "DymoShippingZones");

            migrationBuilder.DropColumn(
                name: "PathaoZoneName",
                table: "DymoShippingZones");

            migrationBuilder.DropColumn(
                name: "PriceCheckedAt",
                table: "DymoShippingZones");

            migrationBuilder.DropColumn(
                name: "PriceError",
                table: "DymoShippingZones");
        }
    }
}
