namespace MarketLink.ViewModels
{
    public class FarmerNotificationsPageViewModel
    {
        public List<FarmerNotificationRowViewModel> Notifications { get; set; } = new();

        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int ReadNotifications { get; set; }
    }

    public class FarmerNotificationRowViewModel
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

    public class FarmerNotificationBellViewModel
    {
        public int UnreadCount { get; set; }
    }
}
