using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class AnomalyDetection
    {
        [Key]
        public int AnomalyDetectionId { get; set; }
        [ForeignKey(nameof(AiModelRun))]
        public int? AiModelRunId { get; set; }
        public AiModelRun? AiModelRun { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal BaselineAverage { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal ObservedDemand { get; set; }
        [Column(TypeName = "decimal(8,3)")]
        public decimal DeviationRatio { get; set; }
        [StringLength(20)]
        public string Severity { get; set; } = "Medium";
        public string Message { get; set; } = "";
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public bool IsAcknowledged { get; set; } = false;
    }
}
