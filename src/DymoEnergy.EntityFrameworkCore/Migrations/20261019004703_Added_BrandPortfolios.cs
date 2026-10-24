using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_BrandPortfolios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DymoBrandPortfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PortalId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Tagline = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ShortIntro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DistributorSince = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BrandColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    StoryHeadline = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HeroKind = table.Column<int>(type: "integer", nullable: false),
                    HeroMediaUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    HeroCaption = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HeroAutoplayMuted = table.Column<bool>(type: "boolean", nullable: false),
                    HeroUsedForSharing = table.Column<bool>(type: "boolean", nullable: false),
                    CtaHeadline = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CtaSubline = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetaTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ContentJson = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_DymoBrandPortfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoBrandPortfolioCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrandPortfolioId = table.Column<int>(type: "integer", nullable: false),
                    Mark = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Covers = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CertificateNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NoExpiry = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FileSize = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoBrandPortfolioCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DymoBrandPortfolioCertificates_DymoBrandPortfolios_BrandPor~",
                        column: x => x.BrandPortfolioId,
                        principalTable: "DymoBrandPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DymoBrandPortfolioDownloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrandPortfolioId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FileSize = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DealersOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoBrandPortfolioDownloads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DymoBrandPortfolioDownloads_DymoBrandPortfolios_BrandPortfo~",
                        column: x => x.BrandPortfolioId,
                        principalTable: "DymoBrandPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DymoBrandPortfolioMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrandPortfolioId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Caption = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoBrandPortfolioMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DymoBrandPortfolioMedia_DymoBrandPortfolios_BrandPortfolioId",
                        column: x => x.BrandPortfolioId,
                        principalTable: "DymoBrandPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DymoBrandPortfolioProductLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrandPortfolioId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Blurb = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    IconKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoBrandPortfolioProductLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DymoBrandPortfolioProductLines_DymoBrandPortfolios_BrandPor~",
                        column: x => x.BrandPortfolioId,
                        principalTable: "DymoBrandPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DymoBrandPortfolioProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrandPortfolioId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ConsentStatus = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoBrandPortfolioProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DymoBrandPortfolioProjects_DymoBrandPortfolios_BrandPortfol~",
                        column: x => x.BrandPortfolioId,
                        principalTable: "DymoBrandPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolioCertificates_BrandPortfolioId",
                table: "DymoBrandPortfolioCertificates",
                column: "BrandPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolioDownloads_BrandPortfolioId",
                table: "DymoBrandPortfolioDownloads",
                column: "BrandPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolioMedia_BrandPortfolioId",
                table: "DymoBrandPortfolioMedia",
                column: "BrandPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolioProductLines_BrandPortfolioId",
                table: "DymoBrandPortfolioProductLines",
                column: "BrandPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolioProjects_BrandPortfolioId",
                table: "DymoBrandPortfolioProjects",
                column: "BrandPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolios_IsPublished",
                table: "DymoBrandPortfolios",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_DymoBrandPortfolios_Slug",
                table: "DymoBrandPortfolios",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DymoBrandPortfolioCertificates");

            migrationBuilder.DropTable(
                name: "DymoBrandPortfolioDownloads");

            migrationBuilder.DropTable(
                name: "DymoBrandPortfolioMedia");

            migrationBuilder.DropTable(
                name: "DymoBrandPortfolioProductLines");

            migrationBuilder.DropTable(
                name: "DymoBrandPortfolioProjects");

            migrationBuilder.DropTable(
                name: "DymoBrandPortfolios");
        }
    }
}
