namespace SmartPotsWeb.Data;

public class HubTelemetry
{
    public int Id { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public float Temp { get; set; }
    public float Hum { get; set; }
    public int Lux { get; set; }
    public bool LightOn { get; set; }
    public bool HumidifierOn { get; set; }

    public List<PotTelemetry> Pots { get; set; } = new();
}

public class PotTelemetry
{
    public int Id { get; set; }
    public int HardwareId { get; set; }
    public int HubTelemetryId { get; set; }
    public HubTelemetry Hub { get; set; } = null!;

    public Guid? PlantProfileId { get; set; }
    public PlantProfile? Profile { get; set; }

    public int Moisture { get; set; }
    public int Target { get; set; }
    public int TargetAir { get; set; }
    public int TargetLux { get; set; }
}

public class Pot
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime? PlantingDate { get; set; }
    public int? HardwareId { get; set; }

    public Guid PlantProfileId { get; set; }
    public PlantProfile Profile { get; set; } = null!;
}