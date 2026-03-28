
namespace SmartPotsWeb.Data
{
    public class PlantProfile
    {
        public Guid Id { get; set; }
        public string SpeciesName { get; set; } = string.Empty;
        public SeasonalityMode Mode { get; set; }
        public SeasonalSettings Spring { get; set; } = new();
        public SeasonalSettings Summer { get; set; } = new();
        public SeasonalSettings Autumn { get; set; } = new();
        public SeasonalSettings Winter { get; set; } = new();

        public SeasonalSettings GetCurrentSettings(Season season)
        {
            return Mode switch
            {
                SeasonalityMode.Static => Spring,

                SeasonalityMode.BiSeasonal => (season == Season.Spring || season == Season.Summer)
                    ? Spring
                    : Autumn,

                SeasonalityMode.QuadSeasonal => season switch
                {
                    Season.Spring => Spring,
                    Season.Summer => Summer,
                    Season.Autumn => Autumn,
                    Season.Winter => Winter,
                    _ => Spring
                },
                _ => Spring
            };
        }
    }
}
