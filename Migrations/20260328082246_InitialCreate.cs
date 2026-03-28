using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartPotsWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HubTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Temp = table.Column<float>(type: "real", nullable: false),
                    Hum = table.Column<float>(type: "real", nullable: false),
                    Lux = table.Column<int>(type: "integer", nullable: false),
                    LightOn = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HubTelemetries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicalPort = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Species = table.Column<string>(type: "text", nullable: false),
                    PhotoUri = table.Column<string>(type: "text", nullable: true),
                    AgeMonths = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    TargetSoilMoisture_Winter = table.Column<int>(type: "integer", nullable: false),
                    TargetSoilMoisture_Spring = table.Column<int>(type: "integer", nullable: false),
                    TargetSoilMoisture_Summer = table.Column<int>(type: "integer", nullable: false),
                    TargetSoilMoisture_Autumn = table.Column<int>(type: "integer", nullable: false),
                    TargetAirHumidity_Winter = table.Column<int>(type: "integer", nullable: false),
                    TargetAirHumidity_Spring = table.Column<int>(type: "integer", nullable: false),
                    TargetAirHumidity_Summer = table.Column<int>(type: "integer", nullable: false),
                    TargetAirHumidity_Autumn = table.Column<int>(type: "integer", nullable: false),
                    TargetLux_Winter = table.Column<int>(type: "integer", nullable: false),
                    TargetLux_Spring = table.Column<int>(type: "integer", nullable: false),
                    TargetLux_Summer = table.Column<int>(type: "integer", nullable: false),
                    TargetLux_Autumn = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PotTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HubTelemetryId = table.Column<int>(type: "integer", nullable: false),
                    PlantProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Moisture = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PotTelemetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PotTelemetries_HubTelemetries_HubTelemetryId",
                        column: x => x.HubTelemetryId,
                        principalTable: "HubTelemetries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PotTelemetries_PlantProfiles_PlantProfileId",
                        column: x => x.PlantProfileId,
                        principalTable: "PlantProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PotTelemetries_HubTelemetryId",
                table: "PotTelemetries",
                column: "HubTelemetryId");

            migrationBuilder.CreateIndex(
                name: "IX_PotTelemetries_PlantProfileId",
                table: "PotTelemetries",
                column: "PlantProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PotTelemetries");

            migrationBuilder.DropTable(
                name: "HubTelemetries");

            migrationBuilder.DropTable(
                name: "PlantProfiles");
        }
    }
}
