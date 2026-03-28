using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartPotsWeb.Data;
using SmartPotsWeb.Models;

namespace SmartPotsWeb.Endpoints;

public static class TelemetryEndpoints
{
    public static void MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telemetry");

        group.MapPost("/", async (AppDbContext db, [FromBody] HubTelemetry incomingData, IHubContext<TelemetryHub> hubContext) =>
        {
            var currentSeason = SeasonHelper.GetCurrentSeason();

            foreach (var pt in incomingData.Pots)
            {
                var physicalPot = await db.Pots
                    .Include(p => p.Profile)
                    .FirstOrDefaultAsync(p => p.HardwareId == pt.HardwareId);

                if (physicalPot != null)
                {
                    var settings = physicalPot.Profile.GetCurrentSettings(currentSeason);
                    pt.Target = settings.SoilMoisture;
                    pt.PlantProfileId = physicalPot.PlantProfileId;
                }
            }

            db.HubTelemetries.Add(incomingData);
            await db.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("ReceiveTelemetryUpdate", incomingData);
            return Results.Ok();
        });
    }
}