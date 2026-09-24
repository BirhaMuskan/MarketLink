using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Review
    {

        [Key]
        public int ReviewId { get; set; }
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [ForeignKey(nameof(Farmer))]
        public int? FarmerId { get; set; }
        public Farmer? Farmer { get; set; }
        [ForeignKey(nameof(FarmerProduct))]
        public int? FarmerProductId { get; set; }
        public FarmerProduct? FarmerProduct { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public bool IsVisible { get; set; } = true;
        public DateTime ReviewDate { get; set; } = DateTime.Now;
    }
}
