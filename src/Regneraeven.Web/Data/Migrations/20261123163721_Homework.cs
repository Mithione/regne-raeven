using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Regneraeven.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Homework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "Problems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomework",
                table: "Problems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Problems_ProfileId_IsHomework_DueDate",
                table: "Problems",
                columns: new[] { "ProfileId", "IsHomework", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Problems_ProfileId_IsHomework_DueDate",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "IsHomework",
                table: "Problems");
        }
    }
}
