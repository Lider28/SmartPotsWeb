using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPotsWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddHubTelemetryIdFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubId",
                table: "PotTelemetries");

            migrationBuilder.RenameColumn(
                name: "HubId",
                table: "PotTelemetries",
                newName: "HubTelemetryId");

            migrationBuilder.RenameIndex(
                name: "IX_PotTelemetries_HubId",
                table: "PotTelemetries",
                newName: "IX_PotTelemetries_HubTelemetryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubTelemetryId",
                table: "PotTelemetries",
                column: "HubTelemetryId",
                principalTable: "HubTelemetries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubTelemetryId",
                table: "PotTelemetries");

            migrationBuilder.RenameColumn(
                name: "HubTelemetryId",
                table: "PotTelemetries",
                newName: "HubId");

            migrationBuilder.RenameIndex(
                name: "IX_PotTelemetries_HubTelemetryId",
                table: "PotTelemetries",
                newName: "IX_PotTelemetries_HubId");

            migrationBuilder.AddForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubId",
                table: "PotTelemetries",
                column: "HubId",
                principalTable: "HubTelemetries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
