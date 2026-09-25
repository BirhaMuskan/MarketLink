namespace MarketLink.DTOs.Farmer
{
    public class FarmerDashboardDto
    {
        public int FarmerId { get; set; }
        public string BusinessName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? ProfileImageUrl { get; set; }

        public int TotalProducts { get; set; }
        public int PendingOrders { get; set; }
        public decimal ThisMonthSales { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public List<DashboardOrderDto> IncomingOrders { get; set; } = new();

        public List<BestSellingProductDto> BestSellingProducts { get; set; } = new();

        public List<StockOverviewDto> StockOverview { get; set; } = new();

        public LatestReviewDto? LatestReview { get; set; }

        public MarketStatusDto? MarketStatus { get; set; }
    }


    public class DashboardOrderDto
    {
        public int OrderId { get; set; }

        public string OrderNo { get; set; } = "";

        public int ItemCount { get; set; }

        public DateTime PickupDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "";

        public string CustomerName { get; set; } = "";
    }


    public class BestSellingProductDto
    {
        public int FarmerProductId { get; set; }

        public string ProductName { get; set; } = "";

        public decimal QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }


    public class StockOverviewDto
    {
        public int FarmerProductId { get; set; }

        public string ProductName { get; set; } = "";

        public string UnitName { get; set; } = "";

        public decimal AvailableQuantity { get; set; }

        public bool IsLowStock { get; set; }
    }


    public class LatestReviewDto
    {
        public int ReviewId { get; set; }

        public string CustomerName { get; set; } = "";

        public decimal Rating { get; set; }

        public string Comment { get; set; } = "";

        public DateTime ReviewDate { get; set; }
    }


    public class MarketStatusDto
    {
        public int FarmerMarketId { get; set; }

        public string MarketName { get; set; } = "";

        public string DayName { get; set; } = "";

        public TimeSpan OpeningTime { get; set; }

        public TimeSpan ClosingTime { get; set; }

        public string Status { get; set; } = "";

        public string? StallNumber { get; set; }
    }
}