using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }
        [ForeignKey(nameof(CreatedByUser))]
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        [Required, StringLength(150)]
        public string Title { get; set; } = "";
        [Required]
        public string Message { get; set; } = "";
        [StringLength(30)]
        public string Audience { get; set; } = "ALL";
        public DateTime PublishFrom { get; set; } = DateTime.Now;
        public DateTime? PublishUntil { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
