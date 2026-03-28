using Microsoft.EntityFrameworkCore;

namespace SmartPotsWeb.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PlantProfile> PlantProfiles { get; set; }
    public DbSet<HubTelemetry> HubTelemetries { get; set; }
    public DbSet<PotTelemetry> PotTelemetries { get; set; }
}