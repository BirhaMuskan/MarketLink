using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models
{
    public class AiModelRun
    {
        [Key]
        public int AiModelRunId { get; set; }
        [Required, StringLength(50)]
        public string ModelType { get; set; } = "";
        [Required, StringLength(100)]
        public string ModelName { get; set; } = "";
        [StringLength(50)]
        public string ModelVersion { get; set; } = "";
        public DateTime? TrainingWindowStart { get; set; }
        public DateTime? TrainingWindowEnd { get; set; }
        public string? ParametersJson { get; set; }
        public string? MetricsJson { get; set; }
        public string? DatasetHash { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt
        {
            get; set;
        }
    }
}
