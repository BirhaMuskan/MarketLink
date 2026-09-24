using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class SystemSetting
    {
        [Key]
        public int SystemSettingId { get; set; }
        [Required, StringLength(100)]
        public string SettingKey { get; set; } = "";
        public string SettingValue { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsPublic { get; set; } = false;
        [ForeignKey(nameof(UpdatedByUser))]
        public int? UpdatedByUserId { get; set; }
        public User? UpdatedByUser { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
