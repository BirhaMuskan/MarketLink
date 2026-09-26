namespace MarketLink.ViewModels
{
    public class AdminCustomersPageViewModel
    {
        public List<AdminCustomerRowViewModel> Customers { get; set; } = new();

        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }

        public string Search { get; set; } = "";
        public string Status { get; set; } = "All";
    }

    public class AdminCustomerRowViewModel
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }

        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";

        public bool IsActive { get; set; }

        public DateTime AccountCreatedAt { get; set; }
        public DateTime CustomerCreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public decimal TotalSpent { get; set; }
    }

    public class AdminCustomerDetailsViewModel
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }

        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";

        public bool IsActive { get; set; }

        public DateTime AccountCreatedAt { get; set; }
        public DateTime CustomerCreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public decimal TotalSpent { get; set; }

        public List<AdminCustomerOrderRowViewModel> RecentOrders { get; set; } = new();
    }

    public class AdminCustomerOrderRowViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public string MarketName { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime PickupDate { get; set; }
    }
}
