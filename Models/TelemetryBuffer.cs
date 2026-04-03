namespace SmartPotsWeb.Models;

using SmartPotsWeb.Data;

public class TelemetryBuffer
{
    private readonly List<HubTelemetry> _buffer = [];
    private readonly Lock _lock = new();

    public void AddReading(HubTelemetry reading)
    {
        lock (_lock)
        {
            _buffer.Add(reading);
        }
    }

    public HubTelemetry? CalculateAverageAndClear()
    {
        lock (_lock)
        {
            if (_buffer.Count == 0) return null;

            var now = DateTime.UtcNow;

            var avgTelemetry = new HubTelemetry
            {
                RecordDate = DateOnly.FromDateTime(now),
                MinuteOfDay = now.Hour * 60 + now.Minute,

                Temp = _buffer.Average(t => t.Temp),
                Hum = _buffer.Average(t => t.Hum),
                Lux = (int)_buffer.Average(t => t.Lux),
                DailyLuxHours = _buffer.Max(t => t.DailyLuxHours),
                LightOn = _buffer.Last().LightOn,
                HumidifierOn = _buffer.Last().HumidifierOn,
                Pots = []
            };
            var allPotsInBuffer = _buffer.SelectMany(h => h.Pots).ToList();
            var groupedPots = allPotsInBuffer.GroupBy(p => p.HardwareId);

            foreach (var group in groupedPots)
            {
                var avgPot = new PotTelemetry
                {
                    HardwareId = group.Key,
                    PlantProfileId = group.Last().PlantProfileId,
                    Moisture = (int)group.Average(p => p.Moisture),

                    Target = group.Last().Target,
                    TargetAir = group.Last().TargetAir,
                    TargetLux = group.Last().TargetLux
                };

                avgTelemetry.Pots.Add(avgPot);
            }

            _buffer.Clear();
            return avgTelemetry;
        }
    }
}