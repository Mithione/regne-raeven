using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regneraeven.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfileUnlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Background",
                table: "Profiles",
                type: "TEXT",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frame",
                table: "Profiles",
                type: "TEXT",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Profiles",
                type: "TEXT",
                maxLength: 24,
                nullable: true);

            // Foxes only: profiles that had a start animal (emoji) become Rævehvalpen.
            migrationBuilder.Sql("UPDATE Profiles SET Avatar = 'fox:hvalp' WHERE Avatar NOT LIKE 'fox:%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Background",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Frame",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Profiles");
        }
    }
}
