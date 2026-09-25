namespace MarketLink.ViewModels
{
    public class FarmerOrdersPageViewModel
    {
        public List<FarmerOrderRowViewModel> Orders { get; set; } = new();

        public int TotalOrders { get; set; }
        public int PlacedOrders { get; set; }
        public int AcceptedOrders { get; set; }
        public int ReadyOrders { get; set; }
        public int CompletedOrders { get; set; }

        public decimal TotalRevenue { get; set; }
    }

    public class FarmerOrderRowViewModel
    {
        public int OrderId { get; set; }

        public string OrderNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";

        public string MarketName { get; set; } = "";
        public string? StallNumber { get; set; }

        public DateTime PickupDate { get; set; }
        public TimeSpan? PickupStartTime { get; set; }
        public TimeSpan? PickupEndTime { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "";
        public string? CustomerNotes { get; set; }
        public string? FarmerNotes { get; set; }

        public DateTime OrderDate { get; set; }

        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class FarmerOrderDetailsViewModel
    {
        public int OrderId { get; set; }

        public string OrderNo { get; set; } = "";
        public string OrderStatus { get; set; } = "";

        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";

        public string MarketName { get; set; } = "";
        public string? StallNumber { get; set; }
        public string? PickupInstructions { get; set; }

        public DateTime PickupDate { get; set; }
        public TimeSpan? PickupStartTime { get; set; }
        public TimeSpan? PickupEndTime { get; set; }

        public decimal TotalAmount { get; set; }

        public string? CustomerNotes { get; set; }
        public string? FarmerNotes { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<FarmerOrderItemViewModel> Items { get; set; } = new();
        public List<FarmerOrderHistoryViewModel> History { get; set; } = new();
    }

    public class FarmerOrderItemViewModel
    {
        public int OrderItemId { get; set; }

        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class FarmerOrderHistoryViewModel
    {
        public string PreviousStatus { get; set; } = "";
        public string NewStatus { get; set; } = "";
        public string? Remarks { get; set; }

        public DateTime ChangedAt { get; set; }

        public string ChangedByName { get; set; } = "";
    }

    public class UpdateFarmerOrderStatusViewModel
    {
        public int OrderId { get; set; }
        public string? FarmerNotes { get; set; }
    }
}
