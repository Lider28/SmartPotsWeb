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

        group.MapPost("/", async (
            AppDbContext db,
            [FromBody] HubTelemetry incomingData,
            IHubContext<TelemetryHub> hubContext,
            TelemetryBuffer buffer) =>
        {
            var now = DateTime.UtcNow;
            var currentSeason = SeasonHelper.GetCurrentSeason();
            incomingData.RecordDate = DateOnly.FromDateTime(now);
            incomingData.MinuteOfDay = now.Hour * 60 + now.Minute;

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

            var currentState = await db.CurrentHubStates.FirstOrDefaultAsync(s => s.Id == 1);
            if (currentState == null)
            {
                currentState = new CurrentHubState { Id = 1 };
                db.CurrentHubStates.Add(currentState);
            }

            currentState.PreviousTemp = currentState.CurrentTemp;
            currentState.PreviousHum = currentState.CurrentHum;
            currentState.PreviousLux = currentState.CurrentLux;

            currentState.CurrentTemp = incomingData.Temp;
            currentState.CurrentHum = incomingData.Hum;
            currentState.CurrentLux = incomingData.Lux;
            currentState.LastUpdatedAt = now;

            await db.SaveChangesAsync();

            if (TelemetryHub.HasViewers)
            {
                await hubContext.Clients.All.SendAsync("ReceiveTelemetryUpdate", incomingData);
            }

            buffer.AddReading(incomingData);

            if (now.Minute == 0 && now.Second < 10 && !TelemetryHub.HasViewers)
            {
                var hourlyAverage = buffer.CalculateAverageAndClear();
                if (hourlyAverage != null)
                {
                    db.HubTelemetries.Add(hourlyAverage);
                    await db.SaveChangesAsync();
                }
            }

            var responseForEsp = new
            {
                lightOn = incomingData.LightOn,
                humidifierOn = incomingData.HumidifierOn,
                pots = incomingData.Pots.Select(p => new
                {
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
            var cutoffDateTime = DateTime.UtcNow.AddHours(-hours);
            var cutoffDate = DateOnly.FromDateTime(cutoffDateTime);
            var cutoffMinute = cutoffDateTime.Hour * 60 + cutoffDateTime.Minute;

            var history = await db.HubTelemetries
                .AsNoTracking()
                .Where(h =>
                    (h.RecordDate > cutoffDate) ||
                    (h.RecordDate == cutoffDate && h.MinuteOfDay >= cutoffMinute))
                .Where(h => h.Pots.Any(p => p.HardwareId == hardwareId))
                .OrderBy(h => h.RecordDate)
                .ThenBy(h => h.MinuteOfDay)
                .Select(h => new
                {
                    Timestamp = h.RecordDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(h.MinuteOfDay))),
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
                PhotoUrl: p.PhotoUrl,
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

        app.MapPost("/api/pots/{hardwareId:int}/image", async (int hardwareId, IFormFile file, AppDbContext db, IWebHostEnvironment env) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("Файл порожній");

            var pot = await db.Pots.Include(p => p.Profile).FirstOrDefaultAsync(p => p.HardwareId == hardwareId);
            if (pot == null) return Results.NotFound();

            var uploadsFolder = Path.Combine(env.WebRootPath, "images", "pots");
            Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/images/pots/{uniqueFileName}";

            pot.PhotoUrl = relativeUrl;
            await db.SaveChangesAsync();

            return Results.Ok(new { url = relativeUrl });
        });
    }
}