namespace MarketLink.ViewModels
{
    public class CustomerOrdersPageViewModel
    {
        public string CurrentFilter { get; set; } = "All";

        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public List<CustomerOrderRowViewModel> Orders { get; set; } = new();
    }

    public class CustomerOrderRowViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";

        public string FarmerName { get; set; } = "";
        public string MarketName { get; set; } = "";

        public DateTime OrderDate { get; set; }
        public DateTime PickupDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "";
        public int ItemCount { get; set; }
    }

    public class CustomerOrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";

        public string FarmerName { get; set; } = "";
        public string MarketName { get; set; } = "";
        public string MarketAddress { get; set; } = "";
        public string? StallNumber { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime PickupDate { get; set; }
        public string PickupTimeText { get; set; } = "";

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "";
        public string? CustomerNotes { get; set; }
        public string? FarmerNotes { get; set; }

        public bool CanCancel { get; set; }
        public string CancellationMessage { get; set; } = "";

        public List<CustomerOrderItemViewModel> Items { get; set; } = new();
        public List<CustomerOrderStatusHistoryViewModel> StatusHistory { get; set; } = new();
    }

    public class CustomerOrderItemViewModel
    {
        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CustomerOrderStatusHistoryViewModel
    {
        public string PreviousStatus { get; set; } = "";
        public string NewStatus { get; set; } = "";
        public string? Remarks { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
