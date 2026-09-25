using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class AdminMarketsPageViewModel
    {
        public List<AdminMarketRowViewModel> Markets { get; set; } = new();

        public int TotalMarkets { get; set; }
        public int ActiveMarkets { get; set; }
        public int InactiveMarkets { get; set; }
    }

    public class AdminMarketRowViewModel
    {
        public int MarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Description { get; set; } = "";

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string MapProvider { get; set; } = "";

        public bool IsActive { get; set; }

        public int OperatingDayCount { get; set; }
        public int FarmerCount { get; set; }

        public string OperatingDaysText { get; set; } = "";
    }

    public class AdminMarketFormViewModel
    {
        public int MarketId { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Market Name")]
        public string MarketName { get; set; } = "";

        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; } = "";

        [Range(-90, 90)]
        [Display(Name = "Latitude")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        [Display(Name = "Longitude")]
        public decimal? Longitude { get; set; }

        [StringLength(30)]
        [Display(Name = "Map Provider")]
        public string MapProvider { get; set; } = "OpenStreetMap";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class AdminMarketDaysPageViewModel
    {
        public int MarketId { get; set; }
        public string MarketName { get; set; } = "";

        public List<AdminMarketDayRowViewModel> Days { get; set; } = new();

        public AdminMarketDayFormViewModel NewDay { get; set; } = new();
    }

    public class AdminMarketDayRowViewModel
    {
        public int MarketDayId { get; set; }
        public string DayName { get; set; } = "";
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminMarketDayFormViewModel
    {
        public int MarketId { get; set; }

        [Required]
        [Display(Name = "Day")]
        public string DayName { get; set; } = "";

        [Required]
        [Display(Name = "Opening Time")]
        public TimeSpan OpeningTime { get; set; }

        [Required]
        [Display(Name = "Closing Time")]
        public TimeSpan ClosingTime { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
