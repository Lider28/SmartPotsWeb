namespace SmartPotsWeb.Data
{
    public record PotChartPoint(
        DateTime Timestamp,
        float HubTemp,
        float HubHum,
        int HubLux,
        int Moisture,
        int TargetMoisture
    );

    public record PotDto(
        int Id,
        string Name,
        int? HardwareId,
        PlantProfileDto Profile
    );

    public record PlantProfileDto(
        string Id,
        string Name,
        string SpeciesName,
        int Mode,
        SeasonalSettingsDto Spring,
        SeasonalSettingsDto Summer,
        SeasonalSettingsDto Autumn,
        SeasonalSettingsDto Winter
    );

    public record SeasonalSettingsDto(
        int SoilMoisture,
        int AirHumidity,
        int LightLux
    );
}
