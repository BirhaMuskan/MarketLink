using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        public DateTime InventoryDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal StockQuantity { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal ReservedQuantity { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal SoldQuantity { get; set; }
        [NotMapped]
        public decimal AvailableQuantity => StockQuantity - ReservedQuantity - SoldQuantity;
        public bool IsSoldOut { get; set; } = false;
        public bool IsAvailable { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
