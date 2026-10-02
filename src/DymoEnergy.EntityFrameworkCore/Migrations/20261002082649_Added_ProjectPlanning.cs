using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DymoEnergy.Migrations
{
    /// <inheritdoc />
    public partial class Added_ProjectPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DymoProjectPlanningSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccentColor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    DueSoonDays = table.Column<int>(type: "integer", nullable: false),
                    PageTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PageSubtitle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NewProjectLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BoardTabLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WeekTabLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AttentionTabLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StagesTabLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StagesIntro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttentionTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ScheduleTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TimeTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TimeFootnote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SeasonTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SeasonSubtitle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoProjectPlanningSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    District = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    StageId = table.Column<int>(type: "integer", nullable: false),
                    StageEnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StatusNote = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    WaitingFor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    WaitingSince = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionLabel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MaterialStatus = table.Column<int>(type: "integer", nullable: false),
                    MaterialNote = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ChecklistDone = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_DymoProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoProjectScheduleEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_DymoProjectScheduleEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoProjectSeasonNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Period = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_DymoProjectSeasonNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoProjectStageHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    StageId = table.Column<int>(type: "integer", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DymoProjectStageHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoProjectStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Responsible = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DurationText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProducesText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TargetDays = table.Column<int>(type: "integer", nullable: true),
                    RequiresScheduling = table.Column<bool>(type: "boolean", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ChecklistText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_DymoProjectStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DymoProjectTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_DymoProjectTeams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjects_StageId",
                table: "DymoProjects",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjects_TeamId",
                table: "DymoProjects",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectScheduleEntries_Date",
                table: "DymoProjectScheduleEntries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectScheduleEntries_ProjectId",
                table: "DymoProjectScheduleEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectScheduleEntries_TeamId",
                table: "DymoProjectScheduleEntries",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectStageHistories_ProjectId",
                table: "DymoProjectStageHistories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectStageHistories_StageId",
                table: "DymoProjectStageHistories",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectStages_Order",
                table: "DymoProjectStages",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_DymoProjectTeams_Order",
                table: "DymoProjectTeams",
                column: "Order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DymoProjectPlanningSettings");

            migrationBuilder.DropTable(
                name: "DymoProjects");

            migrationBuilder.DropTable(
                name: "DymoProjectScheduleEntries");

            migrationBuilder.DropTable(
                name: "DymoProjectSeasonNotes");

            migrationBuilder.DropTable(
                name: "DymoProjectStageHistories");

            migrationBuilder.DropTable(
                name: "DymoProjectStages");

            migrationBuilder.DropTable(
                name: "DymoProjectTeams");
        }
    }
}
