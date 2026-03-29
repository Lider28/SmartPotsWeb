using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPotsWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddAirAndLuxTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetAir",
                table: "PotTelemetries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetLux",
                table: "PotTelemetries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetAir",
                table: "PotTelemetries");

            migrationBuilder.DropColumn(
                name: "TargetLux",
                table: "PotTelemetries");
        }
    }
}
