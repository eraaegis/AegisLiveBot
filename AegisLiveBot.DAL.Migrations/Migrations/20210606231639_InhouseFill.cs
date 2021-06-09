using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisLiveBot.DAL.Migrations.Migrations
{
    public partial class InhouseFill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FillEmoji",
                table: "Inhouses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FillEmoji",
                table: "Inhouses");
        }
    }
}
