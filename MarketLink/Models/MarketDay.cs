using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class MarketDay
    {
        [Key]
        public int MarketDayId { get; set; }
        [ForeignKey(nameof(Market))]
        public int MarketId { get; set; }
        public Market? Market { get; set; }
        [Required, StringLength(20)]
        public string DayName { get; set; } = "";
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
