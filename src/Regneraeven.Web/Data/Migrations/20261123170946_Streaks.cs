using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regneraeven.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Streaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestStreak",
                table: "Profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StreakClaims",
                table: "Profiles",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreakFreezes",
                table: "Profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StreakRewards",
                table: "Profiles",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            // The best streak so far is at least the current one.
            migrationBuilder.Sql("UPDATE Profiles SET BestStreak = Streak WHERE BestStreak < Streak;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestStreak",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "StreakClaims",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "StreakFreezes",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "StreakRewards",
                table: "Profiles");
        }
    }
}
