using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class FarmerMarketDay
    {
        [Key]
        public int FarmerMarketDayId { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        [ForeignKey(nameof(MarketDay))]
        public int MarketDayId { get; set; }
        public MarketDay? MarketDay { get; set; }
        public TimeSpan PickupStartTime { get; set; }
        public TimeSpan PickupEndTime { get; set; }
        public TimeSpan OrderCutoffTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
