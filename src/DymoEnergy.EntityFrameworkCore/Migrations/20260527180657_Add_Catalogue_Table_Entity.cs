using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Add_Catalogue_Table_Entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DymoCatalogues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortalId = table.Column<int>(type: "int", nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroSubtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroCtaText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroCtaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryBackgroundImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThumbnailImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverlayColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverlayOpacity = table.Column<float>(type: "real", nullable: false),
                    PrimaryTextColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccentColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SectionBackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LayoutType = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DymoCatalogues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoCatalogueImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogueId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoCatalogueImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DymoCatalogueImages_DymoCatalogues_CatalogueId",
                        column: x => x.CatalogueId,
                        principalTable: "DymoCatalogues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DymoCatalogueImages_CatalogueId",
                table: "DymoCatalogueImages",
                column: "CatalogueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DymoCatalogueImages");

            migrationBuilder.DropTable(
                name: "DymoCatalogues");
        }
    }
}
