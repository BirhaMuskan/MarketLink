namespace MarketLink.DTOs
{
    public class FarmerDashboardDto
    {
        public int FarmerId { get; set; }
        public string BusinessName { get; set; } = "";

        public int TotalProducts { get; set; }
        public int PendingOrders { get; set; }
        public decimal ThisMonthSales { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public List<FarmerDashboardOrderDto> IncomingOrders { get; set; } = new();
        public List<FarmerTopProductDto> BestSellingProducts { get; set; } = new();
        public List<FarmerStockDto> StockOverview { get; set; } = new();

        public FarmerLatestReviewDto? LatestReview { get; set; }
        public FarmerMarketStatusDto? MarketStatus { get; set; }
    }

    public class FarmerDashboardOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public int ItemCount { get; set; }
        public DateTime PickupDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = "";
    }

    public class FarmerTopProductDto
    {
        public string ProductName { get; set; } = "";
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class FarmerStockDto
    {
        public int InventoryId { get; set; }
        public int FarmerProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public decimal AvailableQuantity { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsSoldOut { get; set; }
    }

    public class FarmerLatestReviewDto
    {
        public int ReviewId { get; set; }
        public string CustomerName { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime ReviewDate { get; set; }
    }

    public class FarmerMarketStatusDto
    {
        public int FarmerMarketId { get; set; }
        public string MarketName { get; set; } = "";
        public string DayName { get; set; } = "";
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public string Status { get; set; } = "";
    }
}
