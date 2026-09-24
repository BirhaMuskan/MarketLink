using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class DemandPrediction
    {
        [Key]
        public int DemandPredictionId { get; set; }
        [ForeignKey(nameof(AiModelRun))]
        public int AiModelRunId { get; set; }
        public AiModelRun? AiModelRun { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        public DateTime PredictionDate { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal PredictedDemand { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal RecommendedStock { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentReservations { get; set; }
        [StringLength(20)]
        public string ShortageRisk { get; set; } = "Low";
        [Column(TypeName = "decimal(6,4)")]
        public decimal? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
