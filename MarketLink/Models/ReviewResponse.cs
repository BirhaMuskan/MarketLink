using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class ReviewResponse
    {
        [Key]
        public int ReviewResponseId { get; set; }
        [ForeignKey(nameof(Review))]
        public int ReviewId { get; set; }
        public Review? Review { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        [Required]
        public string ResponseText { get; set; } = "";
        public DateTime RespondedAt { get; set; } = DateTime.Now;
    }
}
