using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class FavoriteMarket
    {
        [Key]
        public int FavoriteMarketId { get; set; }
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey(nameof(Market))]
        public int MarketId { get; set; }
        public Market? Market { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
