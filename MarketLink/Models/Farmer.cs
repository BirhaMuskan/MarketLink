using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Farmer
    {
        [Key]
        public int FarmerId { get; set; }
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }
        [Required, StringLength(150)]
        public string BusinessName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }
        public string? ProfileImageUrl { get; set; }
        [NotMapped] public IFormFile? ProfileImageFile { get; set; }
        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
