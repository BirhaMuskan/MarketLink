using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        [Required, StringLength(30)]
        public string OrderNo { get; set; } = "";
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        [ForeignKey(nameof(FarmerMarket))]
        public int FarmerMarketId { get; set; }
        public FarmerMarket? FarmerMarket { get; set; }
        [ForeignKey(nameof(PickupSlot))]
        public int? PickupSlotId { get; set; }
        public PickupSlot? PickupSlot { get; set; }
        public DateTime PickupDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Required, StringLength(30)]
        public string OrderStatus { get; set; } = "Placed";
        public string? CustomerNotes { get; set; }
        public string? FarmerNotes { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}
