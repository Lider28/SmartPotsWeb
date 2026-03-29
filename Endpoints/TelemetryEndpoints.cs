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
                    pt.TargetAir = settings.AirHumidity;
                    pt.TargetLux = settings.LightLux;

                    pt.PlantProfileId = physicalPot.PlantProfileId;
                }
            }

            db.HubTelemetries.Add(incomingData);
            await db.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("ReceiveTelemetryUpdate", incomingData);

            var responseForEsp = new
            {
                lightOn = incomingData.LightOn,
                humidifierOn = incomingData.HumidifierOn,
                pots = incomingData.Pots.Select(p => new {
                    port = p.HardwareId,
                    targetSoil = p.Target,
                    targetAir = p.TargetAir,
                    targetLux = p.TargetLux
                })
            };

            return Results.Ok(responseForEsp);
        });

        group.MapGet("/history/{hardwareId:int}", async (AppDbContext db, int hardwareId, [FromQuery] int hours = 24) =>
        {
            var cutoff = DateTime.UtcNow.AddHours(-hours);

            var history = await db.HubTelemetries
                .AsNoTracking()
                .Where(h => h.RecordedAt >= cutoff && h.Pots.Any(p => p.HardwareId == hardwareId))
                .OrderBy(h => h.RecordedAt)
                .Select(h => new
                {
                    Timestamp = h.RecordedAt,
                    Temp = h.Temp,
                    Hum = h.Hum,
                    Lux = h.Lux,
                    DailyLuxHours = h.DailyLuxHours,
                    Pot = h.Pots.FirstOrDefault(p => p.HardwareId == hardwareId)
                })
                .ToListAsync();

            var chartData = history.Select(h => new PotChartPoint(
                Timestamp: h.Timestamp.ToLocalTime(),
                HubTemp: h.Temp,
                HubHum: h.Hum,
                HubLux: h.Lux,
                Moisture: h.Pot?.Moisture ?? 0,
                TargetMoisture: h.Pot?.Target ?? 0,
                DailyLuxHours: h.DailyLuxHours
            )).ToList();

            return Results.Ok(chartData);
        });

        app.MapPost("/api/pots/{hardwareId:int}/settings", async (int hardwareId, PotDto dto, AppDbContext db) =>
        {
            var pot = await db.Pots
                .Include(p => p.Profile)
                .FirstOrDefaultAsync(p => p.HardwareId == hardwareId);

            if (pot == null)
            {
                var newProfile = new PlantProfile
                {
                    Id = Guid.TryParse(dto.Profile.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid(),
                    SpeciesName = dto.Profile.SpeciesName ?? "Невідомий вид",
                    Mode = (SeasonalityMode)dto.Profile.Mode,

                    Spring = new SeasonalSettings { SoilMoisture = dto.Profile.Spring.SoilMoisture, AirHumidity = dto.Profile.Spring.AirHumidity, LightLux = dto.Profile.Spring.LightLux },
                    Summer = new SeasonalSettings { SoilMoisture = dto.Profile.Summer.SoilMoisture, AirHumidity = dto.Profile.Summer.AirHumidity, LightLux = dto.Profile.Summer.LightLux },
                    Autumn = new SeasonalSettings { SoilMoisture = dto.Profile.Autumn.SoilMoisture, AirHumidity = dto.Profile.Autumn.AirHumidity, LightLux = dto.Profile.Autumn.LightLux },
                    Winter = new SeasonalSettings { SoilMoisture = dto.Profile.Winter.SoilMoisture, AirHumidity = dto.Profile.Winter.AirHumidity, LightLux = dto.Profile.Winter.LightLux }
                };

                var newPot = new Pot
                {
                    Name = dto.Name ?? $"Горщик на порту {hardwareId}",
                    HardwareId = hardwareId,
                    Profile = newProfile,
                    PlantingDate = DateTime.UtcNow
                };

                db.PlantProfiles.Add(newProfile);
                db.Pots.Add(newPot);
            }
            else
            {
                pot.Name = dto.Name ?? pot.Name;

                pot.Profile.SpeciesName = dto.Profile.SpeciesName ?? pot.Profile.SpeciesName;
                pot.Profile.Mode = (SeasonalityMode)dto.Profile.Mode;

                pot.Profile.Spring.SoilMoisture = dto.Profile.Spring.SoilMoisture;
                pot.Profile.Spring.AirHumidity = dto.Profile.Spring.AirHumidity;
                pot.Profile.Spring.LightLux = dto.Profile.Spring.LightLux;

                pot.Profile.Summer.SoilMoisture = dto.Profile.Summer.SoilMoisture;
                pot.Profile.Summer.AirHumidity = dto.Profile.Summer.AirHumidity;
                pot.Profile.Summer.LightLux = dto.Profile.Summer.LightLux;

                pot.Profile.Autumn.SoilMoisture = dto.Profile.Autumn.SoilMoisture;
                pot.Profile.Autumn.AirHumidity = dto.Profile.Autumn.AirHumidity;
                pot.Profile.Autumn.LightLux = dto.Profile.Autumn.LightLux;

                pot.Profile.Winter.SoilMoisture = dto.Profile.Winter.SoilMoisture;
                pot.Profile.Winter.AirHumidity = dto.Profile.Winter.AirHumidity;
                pot.Profile.Winter.LightLux = dto.Profile.Winter.LightLux;
            }

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        app.MapGet("/api/pots", async (AppDbContext db) =>
        {
            var pots = await db.Pots
                .Include(p => p.Profile)
                .ToListAsync();

            var dtos = pots.Select(p => new PotDto(
                Id: p.Id,
                Name: p.Name,
                HardwareId: p.HardwareId,
                Profile: new PlantProfileDto(
                    Id: p.Profile.Id.ToString(),
                    Name: p.Name ?? "",
                    SpeciesName: p.Profile.SpeciesName,
                    Mode: (int)p.Profile.Mode,
                    Spring: new SeasonalSettingsDto(p.Profile.Spring.SoilMoisture, p.Profile.Spring.AirHumidity, p.Profile.Spring.LightLux),
                    Summer: new SeasonalSettingsDto(p.Profile.Summer.SoilMoisture, p.Profile.Summer.AirHumidity, p.Profile.Summer.LightLux),
                    Autumn: new SeasonalSettingsDto(p.Profile.Autumn.SoilMoisture, p.Profile.Autumn.AirHumidity, p.Profile.Autumn.LightLux),
                    Winter: new SeasonalSettingsDto(p.Profile.Winter.SoilMoisture, p.Profile.Winter.AirHumidity, p.Profile.Winter.LightLux)
                )
            ));

            return Results.Ok(dtos);
        });

        app.MapDelete("/api/pots/{hardwareId:int}", async (int hardwareId, AppDbContext db) =>
        {
            var pot = await db.Pots
                .Include(p => p.Profile)
                .FirstOrDefaultAsync(p => p.HardwareId == hardwareId);

            if (pot == null)
                return Results.NotFound(new { Message = "Горщик не знайдено" });

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var historyToDelete = await db.Set<PotTelemetry>()
                    .Where(pt => pt.HardwareId == hardwareId)
                    .ToListAsync();

                db.Set<PotTelemetry>().RemoveRange(historyToDelete);
                await db.SaveChangesAsync();

                db.Pots.Remove(pot);
                if (pot.Profile != null)
                {
                    db.PlantProfiles.Remove(pot.Profile);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Results.Ok(new { Message = "Горщик та вся його історія успішно видалені" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Results.Problem(
                    detail: ex.Message,
                    title: "Помилка при видаленні даних з бази"
                );
            }
        });
    }
}