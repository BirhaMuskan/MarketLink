
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Required, StringLength(150)]
        public string ProductName { get; set; } = "";
        public string Description { get; set; } = "";
        public string? DefaultImageUrl { get; set; }
        [NotMapped] public IFormFile? DefaultImageFile { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
