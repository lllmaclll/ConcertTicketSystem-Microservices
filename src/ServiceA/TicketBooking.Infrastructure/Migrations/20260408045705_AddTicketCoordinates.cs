using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoordinateX",
                table: "Tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoordinateY",
                table: "Tickets",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoordinateX",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CoordinateY",
                table: "Tickets");
        }
    }
}
