using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class FarmerProduct
    {
        [Key]
        public int FarmerProductId { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        [ForeignKey(nameof(UnitOfMeasure))]
        public int UnitOfMeasureId { get; set; }
        public UnitOfMeasure? UnitOfMeasure { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public string? FarmerDescription { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsApproved { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
