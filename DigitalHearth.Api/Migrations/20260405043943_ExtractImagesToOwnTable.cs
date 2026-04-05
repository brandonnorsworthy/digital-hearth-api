using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExtractImagesToOwnTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MealLibraryId = table.Column<int>(type: "integer", nullable: false),
                    ImageGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageToken = table.Column<string>(type: "text", nullable: false),
                    ImageData = table.Column<string>(type: "text", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_MealLibrary_MealLibraryId",
                        column: x => x.MealLibraryId,
                        principalTable: "MealLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_MealLibraryId",
                table: "Images",
                column: "MealLibraryId",
                unique: true);

            // Migrate existing image data from MealLibrary into the new Images table
            migrationBuilder.Sql(@"
                INSERT INTO ""Images"" (""MealLibraryId"", ""ImageGuid"", ""ImageToken"", ""ImageData"", ""IsAiGenerated"", ""CreatedAt"", ""UpdatedAt"")
                SELECT ""Id"", gen_random_uuid(), COALESCE(""ImageToken"", encode(gen_random_uuid()::text::bytea, 'hex')), ""ImageData"", true, NOW(), NOW()
                FROM ""MealLibrary""
                WHERE ""ImageData"" IS NOT NULL
            ");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "MealLibrary");

            migrationBuilder.DropColumn(
                name: "ImageToken",
                table: "MealLibrary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.AddColumn<string>(
                name: "ImageData",
                table: "MealLibrary",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageToken",
                table: "MealLibrary",
                type: "text",
                nullable: true);
        }
    }
}
