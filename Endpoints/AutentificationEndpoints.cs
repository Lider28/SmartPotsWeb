using SmartPotsWeb.Data;
using SmartPotsWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace SmartPotsWeb.Endpoints
{
    public static class AutentificationEndpoints
    {
        public static void MapAutentificationEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/notifications/register-token", async (string token, AppDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(token))
                    return Results.BadRequest("Токен не може бути порожнім");

                var existingDevice = await db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token);

                if (existingDevice == null)
                    db.DeviceTokens.Add(new DeviceToken { Token = token });
                else
                    existingDevice.LastUsedAt = DateTime.UtcNow;
                
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Токен успішно збережено" });
            });
        }
    }
}
