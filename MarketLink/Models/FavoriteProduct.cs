using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class FavoriteProduct
    {
        [Key]
        public int FavoriteProductId { get; set; }
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        public bool RestockAlert { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
