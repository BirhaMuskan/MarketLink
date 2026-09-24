using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class SearchHistory
    {
        [Key]
        public int SearchHistoryId { get; set; }
        [ForeignKey(nameof(User))]
        public int? UserId { get; set; }
        public User? User { get; set; }
        [Required, StringLength(300)]
        public string SearchText { get; set; } = "";
        public string? FiltersJson { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }
        public int ResultCount { get; set; }
        public DateTime SearchedAt { get; set; } = DateTime.Now;
    }
}
