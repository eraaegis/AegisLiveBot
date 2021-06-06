using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisLiveBot.DAL.Migrations.Migrations
{
    public partial class InhouseEmoji : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BotEmoji",
                table: "Inhouses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JglEmoji",
                table: "Inhouses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MidEmoji",
                table: "Inhouses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupEmoji",
                table: "Inhouses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopEmoji",
                table: "Inhouses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotEmoji",
                table: "Inhouses");

            migrationBuilder.DropColumn(
                name: "JglEmoji",
                table: "Inhouses");

            migrationBuilder.DropColumn(
                name: "MidEmoji",
                table: "Inhouses");

            migrationBuilder.DropColumn(
                name: "SupEmoji",
                table: "Inhouses");

            migrationBuilder.DropColumn(
                name: "TopEmoji",
                table: "Inhouses");
        }
    }
}
