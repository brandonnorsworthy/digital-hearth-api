using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyImageBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageGenCount",
                table: "Households",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImageGenMonth",
                table: "Households",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyImageBudget",
                table: "Households",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageGenCount",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "ImageGenMonth",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "MonthlyImageBudget",
                table: "Households");
        }
    }
}
