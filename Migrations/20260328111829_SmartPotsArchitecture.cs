using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartPotsWeb.Migrations
{
    /// <inheritdoc />
    public partial class SmartPotsArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubTelemetryId",
                table: "PotTelemetries");

            migrationBuilder.DropColumn(
                name: "AgeMonths",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "PhotoUri",
                table: "PlantProfiles");

            migrationBuilder.DropColumn(
                name: "PhysicalPort",
                table: "PlantProfiles");

            migrationBuilder.RenameColumn(
                name: "HubTelemetryId",
                table: "PotTelemetries",
                newName: "HubId");

            migrationBuilder.RenameIndex(
                name: "IX_PotTelemetries_HubTelemetryId",
                table: "PotTelemetries",
                newName: "IX_PotTelemetries_HubId");

            migrationBuilder.RenameColumn(
                name: "TargetSoilMoisture_Winter",
                table: "PlantProfiles",
                newName: "Winter_SoilMoisture");

            migrationBuilder.RenameColumn(
                name: "TargetSoilMoisture_Summer",
                table: "PlantProfiles",
                newName: "Winter_LightLux");

            migrationBuilder.RenameColumn(
                name: "TargetSoilMoisture_Spring",
                table: "PlantProfiles",
                newName: "Winter_AirHumidity");

            migrationBuilder.RenameColumn(
                name: "TargetSoilMoisture_Autumn",
                table: "PlantProfiles",
                newName: "Summer_SoilMoisture");

            migrationBuilder.RenameColumn(
                name: "TargetLux_Winter",
                table: "PlantProfiles",
                newName: "Summer_LightLux");

            migrationBuilder.RenameColumn(
                name: "TargetLux_Summer",
                table: "PlantProfiles",
                newName: "Summer_AirHumidity");

            migrationBuilder.RenameColumn(
                name: "TargetLux_Spring",
                table: "PlantProfiles",
                newName: "Spring_SoilMoisture");

            migrationBuilder.RenameColumn(
                name: "TargetLux_Autumn",
                table: "PlantProfiles",
                newName: "Spring_LightLux");

            migrationBuilder.RenameColumn(
                name: "TargetAirHumidity_Winter",
                table: "PlantProfiles",
                newName: "Spring_AirHumidity");

            migrationBuilder.RenameColumn(
                name: "TargetAirHumidity_Summer",
                table: "PlantProfiles",
                newName: "Autumn_SoilMoisture");

            migrationBuilder.RenameColumn(
                name: "TargetAirHumidity_Spring",
                table: "PlantProfiles",
                newName: "Autumn_LightLux");

            migrationBuilder.RenameColumn(
                name: "TargetAirHumidity_Autumn",
                table: "PlantProfiles",
                newName: "Autumn_AirHumidity");

            migrationBuilder.RenameColumn(
                name: "Species",
                table: "PlantProfiles",
                newName: "SpeciesName");

            migrationBuilder.AddColumn<int>(
                name: "HardwareId",
                table: "PotTelemetries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Pots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    PlantingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HardwareId = table.Column<int>(type: "integer", nullable: true),
                    PlantProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pots_PlantProfiles_PlantProfileId",
                        column: x => x.PlantProfileId,
                        principalTable: "PlantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pots_PlantProfileId",
                table: "Pots",
                column: "PlantProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubId",
                table: "PotTelemetries",
                column: "HubId",
                principalTable: "HubTelemetries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubId",
                table: "PotTelemetries");

            migrationBuilder.DropTable(
                name: "Pots");

            migrationBuilder.DropColumn(
                name: "HardwareId",
                table: "PotTelemetries");

            migrationBuilder.RenameColumn(
                name: "HubId",
                table: "PotTelemetries",
                newName: "HubTelemetryId");

            migrationBuilder.RenameIndex(
                name: "IX_PotTelemetries_HubId",
                table: "PotTelemetries",
                newName: "IX_PotTelemetries_HubTelemetryId");

            migrationBuilder.RenameColumn(
                name: "Winter_SoilMoisture",
                table: "PlantProfiles",
                newName: "TargetSoilMoisture_Winter");

            migrationBuilder.RenameColumn(
                name: "Winter_LightLux",
                table: "PlantProfiles",
                newName: "TargetSoilMoisture_Summer");

            migrationBuilder.RenameColumn(
                name: "Winter_AirHumidity",
                table: "PlantProfiles",
                newName: "TargetSoilMoisture_Spring");

            migrationBuilder.RenameColumn(
                name: "Summer_SoilMoisture",
                table: "PlantProfiles",
                newName: "TargetSoilMoisture_Autumn");

            migrationBuilder.RenameColumn(
                name: "Summer_LightLux",
                table: "PlantProfiles",
                newName: "TargetLux_Winter");

            migrationBuilder.RenameColumn(
                name: "Summer_AirHumidity",
                table: "PlantProfiles",
                newName: "TargetLux_Summer");

            migrationBuilder.RenameColumn(
                name: "Spring_SoilMoisture",
                table: "PlantProfiles",
                newName: "TargetLux_Spring");

            migrationBuilder.RenameColumn(
                name: "Spring_LightLux",
                table: "PlantProfiles",
                newName: "TargetLux_Autumn");

            migrationBuilder.RenameColumn(
                name: "Spring_AirHumidity",
                table: "PlantProfiles",
                newName: "TargetAirHumidity_Winter");

            migrationBuilder.RenameColumn(
                name: "SpeciesName",
                table: "PlantProfiles",
                newName: "Species");

            migrationBuilder.RenameColumn(
                name: "Autumn_SoilMoisture",
                table: "PlantProfiles",
                newName: "TargetAirHumidity_Summer");

            migrationBuilder.RenameColumn(
                name: "Autumn_LightLux",
                table: "PlantProfiles",
                newName: "TargetAirHumidity_Spring");

            migrationBuilder.RenameColumn(
                name: "Autumn_AirHumidity",
                table: "PlantProfiles",
                newName: "TargetAirHumidity_Autumn");

            migrationBuilder.AddColumn<int>(
                name: "AgeMonths",
                table: "PlantProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PlantProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUri",
                table: "PlantProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhysicalPort",
                table: "PlantProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_PotTelemetries_HubTelemetries_HubTelemetryId",
                table: "PotTelemetries",
                column: "HubTelemetryId",
                principalTable: "HubTelemetries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
