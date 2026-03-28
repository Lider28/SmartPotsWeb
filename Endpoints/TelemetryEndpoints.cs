using Microsoft.AspNetCore.Mvc;
using SmartPotsWeb.Models;

namespace SmartPotsWeb.Endpoints;

public static class TelemetryEndpoints
{
    public static void MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telemetry");

        group.MapPost("/", async (AppDbContext db, [FromBody] HubTelemetry incomingData) =>
        {
            if (incomingData == null) return Results.BadRequest("Дані відсутні");

            incomingData.RecordedAt = DateTime.UtcNow;
            var currentSeason = SeasonHelper.GetCurrentSeason();

            foreach (var pot in incomingData.Pots)
            {
                if (pot.PlantProfileId.HasValue)
                {
                    var profile = await db.PlantProfiles.FindAsync(pot.PlantProfileId);
                    if (profile != null)
                        pot.Target = profile.TargetSoilMoisture.GetFor(currentSeason);
                }
            }

            db.HubTelemetries.Add(incomingData);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Дані збережено", timestamp = incomingData.RecordedAt });
        });
    }
}