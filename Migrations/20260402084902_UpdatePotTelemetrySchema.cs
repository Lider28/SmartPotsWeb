using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPotsWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePotTelemetrySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Pots",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Pots");
        }
    }
}
