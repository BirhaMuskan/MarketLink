using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
