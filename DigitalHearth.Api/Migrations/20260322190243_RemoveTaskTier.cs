using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tier",
                table: "RecurringTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "RecurringTasks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
