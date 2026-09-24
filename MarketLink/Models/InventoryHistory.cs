using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class InventoryHistory
    {
       
        [Key]
        public int InventoryHistoryId { get; set; }
        [ForeignKey(nameof(Inventory))]
        public int InventoryId { get; set; }
        public Inventory? Inventory { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal PreviousQuantity { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal NewQuantity { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityChanged { get; set; }
        [Required, StringLength(40)]
        public string ChangeType { get; set; } = "";
        public string Remarks { get; set; } = "";
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}
