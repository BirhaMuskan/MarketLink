using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MarketLink.ViewModels
{
    public class FarmerMarketsPageViewModel
    {
        public List<FarmerMarketRowViewModel> Markets { get; set; } = new();

        public int TotalMarkets { get; set; }
        public int ActiveMarkets { get; set; }
        public int ScheduledDays { get; set; }
    }

    public class FarmerMarketRowViewModel
    {
        public int FarmerMarketId { get; set; }
        public int MarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";
        public string? StallNumber { get; set; }
        public string? PickupInstructions { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsActive { get; set; }

        public string OperatingDaysText { get; set; } = "";
        public string FarmerScheduleText { get; set; } = "";
        public int ActiveScheduleCount { get; set; }
    }

    public class CreateFarmerMarketViewModel
    {
        [Required]
        [Display(Name = "Market")]
        public int MarketId { get; set; }

        [StringLength(50)]
        [Display(Name = "Stall Number")]
        public string? StallNumber { get; set; }

        [Display(Name = "Pickup Instructions")]
        public string? PickupInstructions { get; set; }

        public List<SelectListItem> Markets { get; set; } = new();
    }

    public class EditFarmerMarketViewModel
    {
        public int FarmerMarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";

        [StringLength(50)]
        [Display(Name = "Stall Number")]
        public string? StallNumber { get; set; }

        [Display(Name = "Pickup Instructions")]
        public string? PickupInstructions { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    public class FarmerMarketSchedulePageViewModel
    {
        public int FarmerMarketId { get; set; }
        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";

        public List<FarmerMarketScheduleRowViewModel> Days { get; set; } = new();
    }

    public class FarmerMarketScheduleRowViewModel
    {
        public int MarketDayId { get; set; }
        public int? FarmerMarketDayId { get; set; }

        public string DayName { get; set; } = "";

        public TimeSpan MarketOpeningTime { get; set; }
        public TimeSpan MarketClosingTime { get; set; }

        public TimeSpan PickupStartTime { get; set; }
        public TimeSpan PickupEndTime { get; set; }
        public TimeSpan OrderCutoffTime { get; set; }

        public bool IsConfigured { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveFarmerMarketDayViewModel
    {
        [Required]
        public int FarmerMarketId { get; set; }

        [Required]
        public int MarketDayId { get; set; }

        [Required]
        [Display(Name = "Pickup Start")]
        public TimeSpan PickupStartTime { get; set; }

        [Required]
        [Display(Name = "Pickup End")]
        public TimeSpan PickupEndTime { get; set; }

        [Required]
        [Display(Name = "Order Cutoff")]
        public TimeSpan OrderCutoffTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
