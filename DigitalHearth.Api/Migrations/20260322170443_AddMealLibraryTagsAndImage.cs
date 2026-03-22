using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMealLibraryTagsAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MealLibrary",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "MealLibrary",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MealLibrary");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "MealLibrary");
        }
    }
}
