using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class PickupSlot
    {
        [Key]
        public int PickupSlotId { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        public DateTime PickupDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaximumOrders { get; set; } = 10;
        public int BookedOrders { get; set; } = 0;
        public bool IsAvailable { get; set; } = true;
    }
}
