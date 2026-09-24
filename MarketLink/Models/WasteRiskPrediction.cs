using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class WasteRiskPrediction
    {
        [Key]
        public int WasteRiskPredictionId { get; set; }
        [ForeignKey(nameof(AiModelRun))]
        public int? AiModelRunId { get; set; }
        public AiModelRun? AiModelRun { get; set; }
        [ForeignKey(nameof(Inventory))]
        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentStock { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal PredictedDemand { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal PredictedExcess { get; set; }
        [StringLength(20)]
        public string RiskLevel { get; set; } = "Low";
        public string Recommendation { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
