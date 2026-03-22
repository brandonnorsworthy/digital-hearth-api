using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameImageUrlToImageData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "MealLibrary",
                newName: "ImageData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageData",
                table: "MealLibrary",
                newName: "ImageUrl");
        }
    }
}
