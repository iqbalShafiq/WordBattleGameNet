using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBattleGame.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignPlayerKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_PlayerStats_PlayerStatsId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_PlayerStats_PlayerId",
                table: "PlayerStats");

            migrationBuilder.DropIndex(
                name: "IX_Players_PlayerStatsId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayerStatsId",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_PlayerId",
                table: "PlayerStats",
                column: "PlayerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerStats_PlayerId",
                table: "PlayerStats");

            migrationBuilder.AddColumn<string>(
                name: "PlayerStatsId",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_PlayerId",
                table: "PlayerStats",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerStatsId",
                table: "Players",
                column: "PlayerStatsId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_PlayerStats_PlayerStatsId",
                table: "Players",
                column: "PlayerStatsId",
                principalTable: "PlayerStats",
                principalColumn: "Id");
        }
    }
}
