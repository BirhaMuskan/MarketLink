using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Market
    {
        [Key]
        public int MarketId { get; set; }
        [Required, StringLength(150)]
        public string MarketName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }
        [StringLength(30)]
        public string MapProvider { get; set; } = "OpenStreetMap";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
