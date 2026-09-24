using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class AiRecommendation
    {
        [Key]
        public int AiRecommendationId { get; set; }
        [ForeignKey(nameof(AiModelRun))]
        public int? AiModelRunId { get; set; }
        public AiModelRun? AiModelRun { get; set; }
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int? FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [Required, StringLength(50)]
        public string RecommendationType { get; set; } = "";
        [Column(TypeName = "decimal(6,4)")]
        public decimal Score { get; set; }
        public string Reason { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt
        {
            get; set;
        }
    }
}
