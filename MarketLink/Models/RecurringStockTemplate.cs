using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class RecurringStockTemplate
    {
        [Key]
        public int RecurringStockTemplateId { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [ForeignKey(nameof(FarmerMarketDay))]
        public int FarmerMarketDayId { get; set; }
        public FarmerMarketDay? FarmerMarketDay { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal DefaultQuantity { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
