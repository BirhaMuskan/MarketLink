using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models
{
    public class UnitOfMeasure
    {
        [Key]
        public int UnitOfMeasureId { get; set; }
        [Required, StringLength(50)]
        public string UnitName { get; set; } = "";
        [Required, StringLength(20)]
        public string UnitCode { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
