using Microsoft.EntityFrameworkCore;
using SmartPotsWeb.Data;

namespace SmartPotsWeb.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<PlantProfile> PlantProfiles { get; set; }
    public DbSet<Pot> Pots { get; set; }
    public DbSet<HubTelemetry> HubTelemetries { get; set; }
    public DbSet<PotTelemetry> PotTelemetries { get; set; }
    public DbSet<CurrentHubState> CurrentHubStates { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlantProfile>().ComplexProperty(p => p.Spring);
        modelBuilder.Entity<PlantProfile>().ComplexProperty(p => p.Summer);
        modelBuilder.Entity<PlantProfile>().ComplexProperty(p => p.Autumn);
        modelBuilder.Entity<PlantProfile>().ComplexProperty(p => p.Winter);
    }
}