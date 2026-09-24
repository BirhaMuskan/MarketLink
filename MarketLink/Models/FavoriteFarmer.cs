using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class FavoriteFarmer
    {
        [Key]
        public int FavoriteFarmerId { get; set; }
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
