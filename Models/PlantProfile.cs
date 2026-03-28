using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SmartPotsWeb.Models
{

    public class PlantProfile
    {
        [Key]
        public Guid Id { get; set; }
        public int PhysicalPort { get; set; }
        public string? Name { get; set; }
        public string Species { get; set; } = string.Empty;
        public string? PhotoUri { get; set; }
        public int AgeMonths { get; set; }
        public ProfileMode Mode { get; set; }

        public SeasonSettings TargetSoilMoisture { get; set; } = new();
        public SeasonSettings TargetAirHumidity { get; set; } = new();
        public SeasonSettings TargetLux { get; set; } = new();
    }

    [Owned]
    public class SeasonSettings
    {
        public int Winter { get; set; }
        public int Spring { get; set; }
        public int Summer { get; set; }
        public int Autumn { get; set; }

        public int GetFor(Season season) => season switch
        {
            Season.Winter => Winter,
            Season.Spring => Spring,
            Season.Summer => Summer,
            Season.Autumn => Autumn,
            _ => 50
        };
    }
}
