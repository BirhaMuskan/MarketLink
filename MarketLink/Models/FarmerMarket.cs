using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class FarmerMarket
    {

        [Key]
        public int FarmerMarketId { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        [ForeignKey(nameof(Market))]
        public int MarketId { get; set; }
        public Market? Market { get; set; }
        [StringLength(50)]
        public string? StallNumber { get; set; }
        public string? PickupInstructions { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}
