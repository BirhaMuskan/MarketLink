using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class GeneratedReport
    {
        [Key]
        public int GeneratedReportId { get; set; }
        [ForeignKey(nameof(GeneratedByUser))]
        public int GeneratedByUserId { get; set; }
        public User? GeneratedByUser { get; set; }
        [Required, StringLength(100)]
        public string ReportType { get; set; } = "";
        public string? ParametersJson { get; set; }
        public string? FileUrl { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}
