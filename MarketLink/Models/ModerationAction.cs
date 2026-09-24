using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class ModerationAction
    {
        [Key]
        public int ModerationActionId { get; set; }
        [ForeignKey(nameof(AdminUser))]
        public int AdminUserId { get; set; }
        public User? AdminUser { get; set; }
        [Required, StringLength(50)]
        public string TargetType { get; set; } = "";
        public int TargetId { get; set; }
        [Required, StringLength(50)]
        public string ActionType { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
