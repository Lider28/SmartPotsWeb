namespace SmartPotsWeb.Data
{
    public class CurrentHubState
    {
        public int Id { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        public float CurrentTemp { get; set; }
        public float CurrentHum { get; set; }
        public int CurrentLux { get; set; }

        public float PreviousTemp { get; set; }
        public float PreviousHum { get; set; }
        public int PreviousLux { get; set; }
    }
}