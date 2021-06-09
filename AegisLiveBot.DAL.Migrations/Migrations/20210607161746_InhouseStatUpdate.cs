using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisLiveBot.DAL.Migrations.Migrations
{
    public partial class InhouseStatUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InhousePlayerStats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<ulong>(nullable: false),
                    Wins = table.Column<int>(nullable: false),
                    Loses = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InhousePlayerStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayersWon = table.Column<string>(nullable: true),
                    PlayersLost = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchHistories", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InhousePlayerStats");

            migrationBuilder.DropTable(
                name: "MatchHistories");
        }
    }
}
