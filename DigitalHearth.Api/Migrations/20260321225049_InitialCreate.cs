using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DigitalHearth.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    JoinCode = table.Column<string>(type: "text", nullable: false),
                    WeekResetDay = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PinHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    HouseholdId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealLibrary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HouseholdId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealLibrary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealLibrary_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealLibrary_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dh = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HouseholdId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCompletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTasks_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringTasks_Users_LastCompletedByUserId",
                        column: x => x.LastCompletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyMeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HouseholdId = table.Column<int>(type: "integer", nullable: false),
                    WeekOf = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MealLibraryId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyMeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyMeals_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyMeals_MealLibrary_MealLibraryId",
                        column: x => x.MealLibraryId,
                        principalTable: "MealLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotifPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TaskId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotifPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotifPreferences_RecurringTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "RecurringTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotifPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCompletions_RecurringTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "RecurringTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCompletions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Households_JoinCode",
                table: "Households",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealLibrary_CreatedByUserId",
                table: "MealLibrary",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MealLibrary_HouseholdId",
                table: "MealLibrary",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_NotifPreferences_TaskId",
                table: "NotifPreferences",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_NotifPreferences_UserId_TaskId",
                table: "NotifPreferences",
                columns: new[] { "UserId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId_Endpoint",
                table: "PushSubscriptions",
                columns: new[] { "UserId", "Endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTasks_HouseholdId",
                table: "RecurringTasks",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTasks_LastCompletedByUserId",
                table: "RecurringTasks",
                column: "LastCompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletions_TaskId",
                table: "TaskCompletions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletions_UserId",
                table: "TaskCompletions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_HouseholdId",
                table: "Users",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyMeals_HouseholdId_WeekOf",
                table: "WeeklyMeals",
                columns: new[] { "HouseholdId", "WeekOf" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyMeals_MealLibraryId",
                table: "WeeklyMeals",
                column: "MealLibraryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotifPreferences");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "TaskCompletions");

            migrationBuilder.DropTable(
                name: "WeeklyMeals");

            migrationBuilder.DropTable(
                name: "RecurringTasks");

            migrationBuilder.DropTable(
                name: "MealLibrary");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Households");
        }
    }
}
