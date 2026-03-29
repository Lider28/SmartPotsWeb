using System.ComponentModel.DataAnnotations.Schema;

namespace SmartPotsWeb.Data
{
    public enum Season { Winter, Spring, Summer, Autumn }
    public enum SeasonalityMode { Static, BiSeasonal, QuadSeasonal }
    public static class SeasonHelper
    {
        public static Season GetCurrentSeason() => DateTime.UtcNow.Month switch
        {
            12 or 1 or 2 => Season.Winter,
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            _ => Season.Autumn
        };
    }

    [ComplexType]
    public class SeasonalSettings
    {
        public int SoilMoisture { get; set; }
        public int AirHumidity { get; set; }
        public int LightLux { get; set; }
    }
}
