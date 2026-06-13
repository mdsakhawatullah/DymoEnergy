using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialMediaToUserSiteSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialFacebookUrl",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialInstagramUrl",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialLinkedinUrl",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialTwitterUrl",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialYoutubeUrl",
                table: "DymoUserSiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialFacebookUrl",
                table: "DymoUserSiteSettings");

            migrationBuilder.DropColumn(
                name: "SocialInstagramUrl",
                table: "DymoUserSiteSettings");

            migrationBuilder.DropColumn(
                name: "SocialLinkedinUrl",
                table: "DymoUserSiteSettings");

            migrationBuilder.DropColumn(
                name: "SocialTwitterUrl",
                table: "DymoUserSiteSettings");

            migrationBuilder.DropColumn(
                name: "SocialYoutubeUrl",
                table: "DymoUserSiteSettings");
        }
    }
}
