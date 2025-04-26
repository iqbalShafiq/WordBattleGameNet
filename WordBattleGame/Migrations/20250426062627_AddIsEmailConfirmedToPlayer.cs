using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordBattleGame.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEmailConfirmedToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "Players");
        }
    }
}
