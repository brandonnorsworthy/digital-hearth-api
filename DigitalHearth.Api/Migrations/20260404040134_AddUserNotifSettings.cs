using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotifSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotifSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TaskReminderHour = table.Column<int>(type: "integer", nullable: true),
                    MediumTermDaysAhead = table.Column<int>(type: "integer", nullable: true),
                    MealPlannerNotifs = table.Column<bool>(type: "boolean", nullable: false),
                    ShortTermTaskNotifs = table.Column<bool>(type: "boolean", nullable: false),
                    MediumTermTaskNotifs = table.Column<bool>(type: "boolean", nullable: false),
                    LongTermTaskNotifs = table.Column<bool>(type: "boolean", nullable: false),
                    TaskCompletedNotifs = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifSettings_UserId",
                table: "UserNotifSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotifSettings");
        }
    }
}
