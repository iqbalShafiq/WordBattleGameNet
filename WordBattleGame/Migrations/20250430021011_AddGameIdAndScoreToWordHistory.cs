using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBattleGame.Migrations
{
    /// <inheritdoc />
    public partial class AddGameIdAndScoreToWordHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameId",
                table: "WordHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "WordHistories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoundId",
                table: "WordHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "WordHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameId",
                table: "WordHistories");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "WordHistories");

            migrationBuilder.DropColumn(
                name: "RoundId",
                table: "WordHistories");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "WordHistories");
        }
    }
}
