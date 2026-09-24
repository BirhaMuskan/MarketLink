using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class ProductImage
    {
        [Key]
        public int ProductImageId { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        public string ImageUrl { get; set; } = "";
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
