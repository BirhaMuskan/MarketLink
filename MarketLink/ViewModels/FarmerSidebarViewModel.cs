namespace MarketLink.ViewModels
{
    public class FarmerSidebarViewModel
    {
        public int FarmerId { get; set; }
        public string BusinessName { get; set; } = "Farmer";
        public string FullName { get; set; } = "";
        public string? ProfileImageUrl { get; set; }

        public int ProductCount { get; set; }
        public int PendingOrderCount { get; set; }
        public int UnreadNotificationCount { get; set; }

        public string Initial =>
            string.IsNullOrWhiteSpace(BusinessName)
                ? "F"
                : BusinessName.Trim()[0].ToString().ToUpper();
    }
}
