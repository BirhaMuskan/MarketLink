using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class AssistantMessage
    {
        [Key]
        public int AssistantMessageId { get; set; }
        [ForeignKey(nameof(AssistantConversation))]
        public int AssistantConversationId { get; set; }
        public AssistantConversation? AssistantConversation { get; set; }
        [Required, StringLength(20)]
        public string MessageRole { get; set; } = "user";
        [Required]
        public string MessageText { get; set; } = "";
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
