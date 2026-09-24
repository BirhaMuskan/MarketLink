using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        [ForeignKey(nameof(User))]
        public int? UserId { get; set; }
        public User? User { get; set; }
        [Required, StringLength(100)]
        public string Action { get; set; } = "";
        [StringLength(100)]
        public string EntityName { get; set; } = "";
        [StringLength(100)]
        public string EntityId { get; set; } = "";
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
