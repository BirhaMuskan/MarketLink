using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [ForeignKey(nameof(Inventory))]
        public int? InventoryId { get; set; }
        public Inventory? Inventory { get; set; }
        [Required, StringLength(150)]
        public string ProductName { get; set; } = "";
        [StringLength(30)]
        public string UnitName { get; set; } = "";
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice {get; set;}
        }
}
