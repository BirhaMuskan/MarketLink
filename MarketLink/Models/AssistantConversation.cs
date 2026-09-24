using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class AssistantConversation
    {
        [Key]
        public int AssistantConversationId { get; set; }
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }
        [StringLength(150)]
        public string Title { get; set; } = "";
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime LastActivityAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
