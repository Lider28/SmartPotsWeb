using System.ComponentModel.DataAnnotations;

namespace SmartPotsWeb.Data
{
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    }
}
