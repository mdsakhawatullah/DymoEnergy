using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <summary>
    /// Catalogue → Category rename.
    ///
    /// EF scaffolded this as DROP TABLE + CREATE TABLE, which would have discarded
    /// every category and category image. Rewritten as in-place renames so the
    /// existing rows, keys and indexes carry over untouched.
    /// </summary>
    public partial class Rename_Catalogue_To_Category : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Columns (renamed while the tables still carry their old names) ──
            migrationBuilder.RenameColumn(
                name: "CatalogueId",
                table: "DymoCatalogueImages",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "CatalogueId",
                table: "DymoProducts",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "CataloguesTitle",
                table: "DymoUserSiteSettings",
                newName: "CategoriesTitle");

            migrationBuilder.RenameColumn(
                name: "CataloguesDescription",
                table: "DymoUserSiteSettings",
                newName: "CategoriesDescription");

            // ── Tables ─────────────────────────────────────────────────────────
            migrationBuilder.RenameTable(
                name: "DymoCatalogues",
                newName: "DymoCategories");

            migrationBuilder.RenameTable(
                name: "DymoCatalogueImages",
                newName: "DymoCategoryImages");

            // ── Index ──────────────────────────────────────────────────────────
            migrationBuilder.RenameIndex(
                name: "IX_DymoCatalogueImages_CatalogueId",
                table: "DymoCategoryImages",
                newName: "IX_DymoCategoryImages_CategoryId");

            // ── Constraints ────────────────────────────────────────────────────
            // Guarded: a constraint named differently in an older database should
            // not abort the migration, it just keeps its existing name.
            RenameConstraint(migrationBuilder, "DymoCategories",      "PK_DymoCatalogues",      "PK_DymoCategories");
            RenameConstraint(migrationBuilder, "DymoCategoryImages",  "PK_DymoCatalogueImages", "PK_DymoCategoryImages");
            RenameConstraint(migrationBuilder, "DymoCategoryImages",
                "FK_DymoCatalogueImages_DymoCatalogues_CatalogueId",
                "FK_DymoCategoryImages_DymoCategories_CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RenameConstraint(migrationBuilder, "DymoCategoryImages",
                "FK_DymoCategoryImages_DymoCategories_CategoryId",
                "FK_DymoCatalogueImages_DymoCatalogues_CatalogueId");
            RenameConstraint(migrationBuilder, "DymoCategoryImages", "PK_DymoCategoryImages", "PK_DymoCatalogueImages");
            RenameConstraint(migrationBuilder, "DymoCategories",     "PK_DymoCategories",     "PK_DymoCatalogues");

            migrationBuilder.RenameIndex(
                name: "IX_DymoCategoryImages_CategoryId",
                table: "DymoCategoryImages",
                newName: "IX_DymoCatalogueImages_CatalogueId");

            migrationBuilder.RenameTable(
                name: "DymoCategoryImages",
                newName: "DymoCatalogueImages");

            migrationBuilder.RenameTable(
                name: "DymoCategories",
                newName: "DymoCatalogues");

            migrationBuilder.RenameColumn(
                name: "CategoriesDescription",
                table: "DymoUserSiteSettings",
                newName: "CataloguesDescription");

            migrationBuilder.RenameColumn(
                name: "CategoriesTitle",
                table: "DymoUserSiteSettings",
                newName: "CataloguesTitle");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "DymoProducts",
                newName: "CatalogueId");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "DymoCatalogueImages",
                newName: "CatalogueId");
        }

        private static void RenameConstraint(MigrationBuilder b, string table, string from, string to)
        {
            b.Sql($@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '{from}') THEN
                        ALTER TABLE ""{table}"" RENAME CONSTRAINT ""{from}"" TO ""{to}"";
                    END IF;
                END $$;");
        }
    }
}
