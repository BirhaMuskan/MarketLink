using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class NotificationPreference
    {
        [Key]
        public int NotificationPreferenceId { get; set; }
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }
        public bool InAppEnabled { get; set; } = true;
        public bool EmailEnabled { get; set; } = true;
        public bool OrderUpdates { get; set; } = true;
        public bool PickupReminders { get; set; } = true;
        public bool RestockAlerts { get; set; } = true;
        public bool AiAlerts { get; set; } = true;
    }
}
