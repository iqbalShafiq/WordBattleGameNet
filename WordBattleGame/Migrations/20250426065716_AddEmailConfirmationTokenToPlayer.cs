using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBattleGame.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmationTokenToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiry",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiry",
                table: "Players");
        }
    }
}
