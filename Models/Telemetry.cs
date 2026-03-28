namespace SmartPotsWeb.Models;

public class HubTelemetry
{
    public int Id { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public float Temp { get; set; }
    public float Hum { get; set; }
    public int Lux { get; set; }
    public bool LightOn { get; set; }

    public List<PotTelemetry> Pots { get; set; } = new();
}

public class PotTelemetry
{
    public int Id { get; set; }

    public int HubTelemetryId { get; set; }
    public HubTelemetry Hub { get; set; } = null!;

    public Guid? PlantProfileId { get; set; }
    public PlantProfile? Profile { get; set; }

    public int Moisture { get; set; }
    public int Target { get; set; }
}