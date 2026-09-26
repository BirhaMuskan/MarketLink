namespace MarketLink.ViewModels
{
    public class CustomerNotificationsPageViewModel
    {
        public List<CustomerNotificationRowViewModel> Notifications { get; set; } = new();

        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int ReadNotifications { get; set; }

        public string CurrentFilter { get; set; } = "All";
    }

    public class CustomerNotificationRowViewModel
    {
        public int NotificationId { get; set; }

        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string NotificationType { get; set; } = "";
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class CustomerNotificationBellViewModel
    {
        public int UnreadCount { get; set; }
    }

    public class CustomerFavoritesPageViewModel
    {
        public int FavoriteFarmersCount { get; set; }
        public int FavoriteProductsCount { get; set; }
        public int FavoriteMarketsCount { get; set; }

        public List<CustomerFavoriteFarmerViewModel> Farmers { get; set; } = new();
        public List<CustomerFavoriteProductPageViewModel> Products { get; set; } = new();
        public List<CustomerFavoriteMarketPageViewModel> Markets { get; set; } = new();
    }

    public class CustomerFavoriteFarmerViewModel
    {
        public int FavoriteFarmerId { get; set; }
        public int FarmerId { get; set; }

        public string BusinessName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ProfileImageUrl { get; set; }
    }

    public class CustomerFavoriteProductPageViewModel
    {
        public int FavoriteProductId { get; set; }
        public int FarmerProductId { get; set; }
        public int FarmerId { get; set; }

        public string ProductName { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }
        public decimal AvailableQuantity { get; set; }

        public bool IsAvailable { get; set; }
        public bool RestockAlert { get; set; }
    }

    public class CustomerFavoriteMarketPageViewModel
    {
        public int FavoriteMarketId { get; set; }
        public int MarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";
        public string OperatingDaysText { get; set; } = "";
    }
}
