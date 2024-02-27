using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VibrationMonitorReservation.Migrations
{
    public partial class EmailsActivatedAddedToAspNetUsersTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailsActivated",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailsActivated",
                table: "AspNetUsers");
        }
    }
}
