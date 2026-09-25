namespace MarketLink.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public string CustomerName { get; set; } = "Customer";

        public int TotalOrders { get; set; }
        public int OrdersThisMonth { get; set; }
        public int ActiveOrders { get; set; }

        public int FavoriteProducts { get; set; }
        public int SavedMarkets { get; set; }

        public List<CustomerRecentOrderViewModel> RecentOrders { get; set; } = new();
        public List<CustomerFavoriteProductViewModel> FavoriteProductRows { get; set; } = new();
        public List<CustomerSavedMarketViewModel> SavedMarketRows { get; set; } = new();
    }

    public class CustomerRecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = "";
    }

    public class CustomerFavoriteProductViewModel
    {
        public int FarmerProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CustomerSavedMarketViewModel
    {
        public int MarketId { get; set; }
        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";
    }
}
