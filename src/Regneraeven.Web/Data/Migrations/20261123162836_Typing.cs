using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regneraeven.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Typing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TypingResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    LessonId = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    Stars = table.Column<int>(type: "INTEGER", nullable: false),
                    Accuracy = table.Column<int>(type: "INTEGER", nullable: false),
                    Wpm = table.Column<int>(type: "INTEGER", nullable: false),
                    XpAwarded = table.Column<int>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypingResults_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TypingResults_ProfileId_LessonId",
                table: "TypingResults",
                columns: new[] { "ProfileId", "LessonId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TypingResults");
        }
    }
}
