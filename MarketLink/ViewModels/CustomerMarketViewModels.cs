namespace MarketLink.ViewModels
{
    public class CustomerMarketsPageViewModel
    {
        public List<CustomerMarketCardViewModel> Markets { get; set; } = new();

        public int TotalMarkets { get; set; }
        public int SavedMarkets { get; set; }
    }

    public class CustomerMarketCardViewModel
    {
        public int MarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string OperatingDaysText { get; set; } = "";
        public int FarmerCount { get; set; }

        public bool IsFavorite { get; set; }
    }

    public class CustomerMarketDetailsViewModel
    {
        public int MarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsFavorite { get; set; }

        public List<CustomerMarketDayViewModel> OperatingDays { get; set; } = new();
        public List<CustomerMarketFarmerViewModel> Farmers { get; set; } = new();
    }

    public class CustomerMarketDayViewModel
    {
        public string DayName { get; set; } = "";
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
    }

    public class CustomerMarketFarmerViewModel
    {
        public int FarmerId { get; set; }
        public string BusinessName { get; set; } = "";
        public string? StallNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string Description { get; set; } = "";
    }
}
