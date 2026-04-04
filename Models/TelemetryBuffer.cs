using global::SmartPotsWeb.Data;

namespace SmartPotsWeb.Models;
public class TelemetryBuffer
{
    private readonly List<HubTelemetry> _buffer = [];
    private readonly Lock _lock = new();

    private int _currentHour = DateTime.UtcNow.Hour;

    public HubTelemetry? AddAndCheckIfHourChanged(HubTelemetry incomingData)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now.Hour != _currentHour)
            {
                var hourlyAverage = CalculateAverageInternal(now.AddHours(-1));
                _buffer.Clear();
                _buffer.Add(incomingData);
                _currentHour = now.Hour;
                return hourlyAverage;
            }
            else
            {
                _buffer.Add(incomingData);
                return null;
            }
        }
    }

    private HubTelemetry? CalculateAverageInternal(DateTime targetTime)
    {
        if (_buffer.Count == 0) return null;

        var avgTelemetry = new HubTelemetry
        {
            RecordDate = DateOnly.FromDateTime(targetTime),
            MinuteOfDay = targetTime.Hour * 60,
            Temp = _buffer.Average(t => t.Temp),
            Hum = _buffer.Average(t => t.Hum),
            Lux = (int)_buffer.Average(t => t.Lux),
            DailyLuxHours = _buffer.Max(t => t.DailyLuxHours),
            LightOn = _buffer.Last().LightOn,
            HumidifierOn = _buffer.Last().HumidifierOn,
            Pots = []
        };

        var potsInGroup = _buffer.SelectMany(h => h.Pots).GroupBy(p => p.HardwareId);
        foreach (var potGroup in potsInGroup)
        {
            avgTelemetry.Pots.Add(new PotTelemetry
            {
                HardwareId = potGroup.Key,
                PlantProfileId = potGroup.Last().PlantProfileId,
                Moisture = (int)potGroup.Average(p => p.Moisture),
                Target = (int)potGroup.Average(p => p.Target),
                TargetAir = (int)potGroup.Average(p => p.TargetAir),
                TargetLux = (int)potGroup.Average(p => p.TargetLux)
            });
        }

        return avgTelemetry;
    }
}