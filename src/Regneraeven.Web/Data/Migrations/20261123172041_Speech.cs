using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regneraeven.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Speech : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpeechRate",
                table: "Profiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "rolig");

            migrationBuilder.AddColumn<string>(
                name: "Voice",
                table: "Profiles",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "kvinde");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpeechRate",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Voice",
                table: "Profiles");
        }
    }
}
