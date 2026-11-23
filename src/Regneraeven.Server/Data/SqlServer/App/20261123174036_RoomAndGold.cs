using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regneraeven.Server.Data.SqlServer.App
{
    /// <inheritdoc />
    public partial class RoomAndGold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Gold",
                table: "XpEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gold",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "Profiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomPurchases_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomPurchases_ProfileId_ItemId",
                table: "RoomPurchases",
                columns: new[] { "ProfileId", "ItemId" },
                unique: true);

            // Starting gold for children who already earned XP: 1 gold per 4 XP, rounded up.
            migrationBuilder.Sql("UPDATE Profiles SET Gold = (Xp + 3) / 4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomPurchases");

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "XpEvents");

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Room",
                table: "Profiles");
        }
    }
}
