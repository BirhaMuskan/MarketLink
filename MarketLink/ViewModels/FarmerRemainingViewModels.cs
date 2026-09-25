using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MarketLink.ViewModels
{
    // =========================================================
    // SALES / ORDER HISTORY
    // =========================================================
    public class FarmerSalesPageViewModel
    {
        public decimal CompletedRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<FarmerSalesProductViewModel> BestSellingProducts { get; set; } = new();
        public List<FarmerSalesOrderViewModel> RecentCompletedOrders { get; set; } = new();
    }

    public class FarmerSalesProductViewModel
    {
        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class FarmerSalesOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string MarketName { get; set; } = "";

        public DateTime OrderDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        public decimal TotalAmount { get; set; }
    }

    // =========================================================
    // REVIEWS
    // =========================================================
    public class FarmerReviewsPageViewModel
    {
        public List<FarmerReviewRowViewModel> Reviews { get; set; } = new();

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int UnansweredReviews { get; set; }
    }

    public class FarmerReviewRowViewModel
    {
        public int ReviewId { get; set; }

        public string CustomerName { get; set; } = "";
        public string OrderNo { get; set; } = "";

        public string TargetName { get; set; } = "";

        public int Rating { get; set; }
        public string Comment { get; set; } = "";

        public DateTime ReviewDate { get; set; }

        public string? ResponseText { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class FarmerReviewResponseViewModel
    {
        public int ReviewId { get; set; }

        public string CustomerName { get; set; } = "";
        public string OrderNo { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";

        [Required]
        [StringLength(2000)]
        [Display(Name = "Your Response")]
        public string ResponseText { get; set; } = "";
    }

    // =========================================================
    // PROFILE
    // =========================================================
    public class FarmerProfileViewModel
    {
        public int FarmerId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string Phone { get; set; } = "";

        [Required]
        [StringLength(150)]
        [Display(Name = "Business / Stall Name")]
        public string BusinessName { get; set; } = "";

        public string Description { get; set; } = "";

        [Display(Name = "Business Address")]
        public string Address { get; set; } = "";

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImageFile { get; set; }

        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
    }
}
