using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartPotsWeb.Migrations
{
    /// <inheritdoc />
    public partial class ApplyTelemetryOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinuteOfDay",
                table: "HubTelemetries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecordDate",
                table: "HubTelemetries",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "CurrentHubStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentTemp = table.Column<float>(type: "real", nullable: false),
                    CurrentHum = table.Column<float>(type: "real", nullable: false),
                    CurrentLux = table.Column<int>(type: "integer", nullable: false),
                    PreviousTemp = table.Column<float>(type: "real", nullable: false),
                    PreviousHum = table.Column<float>(type: "real", nullable: false),
                    PreviousLux = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentHubStates", x => x.Id);
                });

            migrationBuilder.Sql(@"
                UPDATE ""HubTelemetries"" 
                SET 
                    ""RecordDate"" = ""RecordedAt""::date, 
                    ""MinuteOfDay"" = (EXTRACT(hour FROM ""RecordedAt"") * 60) + EXTRACT(minute FROM ""RecordedAt"");

                INSERT INTO ""CurrentHubStates"" (""CurrentTemp"", ""CurrentHum"", ""CurrentLux"", ""PreviousTemp"", ""PreviousHum"", ""PreviousLux"", ""LastUpdatedAt"")
                SELECT ""Temp"", ""Hum"", ""Lux"", ""Temp"", ""Hum"", ""Lux"", NOW()
                FROM ""HubTelemetries""
                ORDER BY ""RecordedAt"" DESC
                LIMIT 1;
            ");

            migrationBuilder.DropColumn(
                name: "RecordedAt",
                table: "HubTelemetries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentHubStates");

            migrationBuilder.DropColumn(
                name: "MinuteOfDay",
                table: "HubTelemetries");

            migrationBuilder.DropColumn(
                name: "RecordDate",
                table: "HubTelemetries");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordedAt",
                table: "HubTelemetries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}