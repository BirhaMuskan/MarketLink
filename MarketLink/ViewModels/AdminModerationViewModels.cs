namespace MarketLink.ViewModels
{
    public class AdminModerationPageViewModel
    {
        public string Tab { get; set; } = "Products";
        public string Search { get; set; } = "";

        public int TotalProducts { get; set; }
        public int HiddenProducts { get; set; }
        public int TotalReviews { get; set; }
        public int HiddenReviews { get; set; }

        public List<AdminModerationProductViewModel> Products { get; set; } = new();
        public List<AdminModerationReviewViewModel> Reviews { get; set; } = new();
    }

    public class AdminModerationProductViewModel
    {
        public int FarmerProductId { get; set; }
        public int FarmerId { get; set; }

        public string ProductName { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal Price { get; set; }

        public string FarmerDescription { get; set; } = "";

        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class AdminModerationReviewViewModel
    {
        public int ReviewId { get; set; }
        public int OrderId { get; set; }

        public string CustomerName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public string OrderNo { get; set; } = "";

        public int Rating { get; set; }
        public string Comment { get; set; } = "";

        public bool IsVisible { get; set; }

        public DateTime ReviewDate { get; set; }
    }
}
