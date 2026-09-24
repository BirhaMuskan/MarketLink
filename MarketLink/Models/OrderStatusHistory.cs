using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class OrderStatusHistory
    {
        
        [Key]
        public int OrderStatusHistoryId { get; set; }
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [StringLength(30)]
        public string PreviousStatus { get; set; } = "";
        [Required, StringLength(30)]
        public string NewStatus { get; set; } = "";
        [ForeignKey(nameof(ChangedByUser))]
        public int? ChangedByUserId { get; set; }
        public User? ChangedByUser { get; set; }
        public string? Remarks { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}
