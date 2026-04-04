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
            incomingData.RecordDate = DateOnly.FromDateTime(now);
            incomingData.MinuteOfDay = now.Hour * 60 + now.Minute;

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

            var readyHourlyRecord = buffer.AddAndCheckIfHourChanged(incomingData);

            if (readyHourlyRecord != null)
            {
                db.HubTelemetries.Add(readyHourlyRecord);
                await db.SaveChangesAsync();
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
        }); ;

        group.MapGet("/history/{hardwareId:int}", async (AppDbContext db, int hardwareId, [FromQuery] string range = "DAY") =>
        {
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
            }
            catch
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }

            var utcNow = DateTime.UtcNow;
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

            DateTime localStartDate;
            DateTime localEndDate;

            if (range.Equals("MONTH", StringComparison.OrdinalIgnoreCase))
            {
                localStartDate = new DateTime(localNow.Year, localNow.Month, 1);
                localEndDate = localStartDate.AddMonths(1);
            }
            else if (range.Equals("WEEK", StringComparison.OrdinalIgnoreCase))
            {
                int diff = (7 + (localNow.DayOfWeek - DayOfWeek.Monday)) % 7;
                localStartDate = localNow.Date.AddDays(-diff);
                localEndDate = localStartDate.AddDays(7);
            }
            else
            {
                localStartDate = localNow.Date;
                localEndDate = localStartDate.AddDays(1);
            }

            var utcStartDate = TimeZoneInfo.ConvertTimeToUtc(localStartDate, tz);
            var utcEndDate = TimeZoneInfo.ConvertTimeToUtc(localEndDate, tz);

            var startFilter = DateOnly.FromDateTime(utcStartDate.AddDays(-1));
            var endFilter = DateOnly.FromDateTime(utcEndDate.AddDays(1));

            var rawData = await db.HubTelemetries
                .AsNoTracking()
                .Where(h => h.RecordDate >= startFilter && h.RecordDate <= endFilter)
                .Where(h => h.Pots.Any(p => p.HardwareId == hardwareId))
                .Select(h => new
                {
                    h.RecordDate,
                    h.MinuteOfDay,
                    h.Temp,
                    h.Hum,
                    h.Lux,
                    h.DailyLuxHours,
                    Pot = h.Pots.FirstOrDefault(p => p.HardwareId == hardwareId)
                })
                .ToListAsync();

            var historyWithTime = rawData
                .Select(h => new
                {
                    Data = h,
                    UtcTime = DateTime.SpecifyKind(h.RecordDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(h.MinuteOfDay))), DateTimeKind.Utc)
                })
                .Select(x => new
                {
                    x.Data,
                    x.UtcTime,
                    LocalTime = TimeZoneInfo.ConvertTimeFromUtc(x.UtcTime, tz)
                })
                .Where(x => x.UtcTime >= utcStartDate && x.UtcTime < utcEndDate)
                .ToList();

            List<PotChartPoint> chartData;

            if (range.Equals("DAY", StringComparison.OrdinalIgnoreCase))
            {
                chartData = [.. historyWithTime.Select(x => new PotChartPoint(
            Timestamp: x.UtcTime,
            HubTemp: x.Data.Temp,
            HubHum: x.Data.Hum,
            HubLux: x.Data.Lux,
            Moisture: x.Data.Pot?.Moisture ?? 0,
            TargetMoisture: x.Data.Pot?.Target ?? 0,
            DailyLuxHours: x.Data.DailyLuxHours
        )).OrderBy(c => c.Timestamp)];
            }
            else
            {
                chartData = [.. historyWithTime
            .GroupBy(x => x.LocalTime.Date)
            .Select(group => new PotChartPoint(
                Timestamp: TimeZoneInfo.ConvertTimeToUtc(group.Key, tz),
                HubTemp: group.Average(x => x.Data.Temp),
                HubHum: group.Average(x => x.Data.Hum),
                HubLux: (int)group.Average(x => x.Data.Lux),
                Moisture: (int)group.Average(x => x.Data.Pot?.Moisture ?? 0),
                TargetMoisture: (int)group.Average(x => x.Data.Pot?.Target ?? 0),
                DailyLuxHours: group.Max(x => x.Data.DailyLuxHours)
            ))
            .OrderBy(c => c.Timestamp)];
            }

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