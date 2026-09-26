using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class CustomerReviewsPageViewModel
    {
        public int CompletedOrders { get; set; }
        public int ReviewsGiven { get; set; }

        public List<CustomerReviewOrderViewModel> Orders { get; set; } = new();
        public List<CustomerReviewHistoryViewModel> Reviews { get; set; } = new();
    }

    public class CustomerReviewOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";

        public int FarmerId { get; set; }
        public string FarmerName { get; set; } = "";

        public DateTime PickupDate { get; set; }

        public bool FarmerReviewed { get; set; }

        public List<CustomerReviewProductTargetViewModel> Products { get; set; } = new();
    }

    public class CustomerReviewProductTargetViewModel
    {
        public int FarmerProductId { get; set; }
        public string ProductName { get; set; } = "";
        public bool Reviewed { get; set; }
    }

    public class CustomerCreateReviewViewModel
    {
        public int OrderId { get; set; }

        public int? FarmerId { get; set; }
        public int? FarmerProductId { get; set; }

        public string TargetName { get; set; } = "";
        public string OrderNo { get; set; } = "";

        [Range(1, 5)]
        [Display(Name = "Rating")]
        public int Rating { get; set; } = 5;

        [Required]
        [StringLength(1500)]
        public string Comment { get; set; } = "";
    }

    public class CustomerEditReviewViewModel
    {
        public int ReviewId { get; set; }

        public string TargetName { get; set; } = "";
        public string OrderNo { get; set; } = "";

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1500)]
        public string Comment { get; set; } = "";
    }

    public class CustomerReviewHistoryViewModel
    {
        public int ReviewId { get; set; }

        public string TargetName { get; set; } = "";
        public string OrderNo { get; set; } = "";

        public int Rating { get; set; }
        public string Comment { get; set; } = "";

        public DateTime ReviewDate { get; set; }

        public string? FarmerResponse { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
