using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyMealIsCooked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCooked",
                table: "WeeklyMeals",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCooked",
                table: "WeeklyMeals");
        }
    }
}
