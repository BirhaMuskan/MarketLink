using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MarketLink.ViewModels
{
    public class FarmerPickupSlotsPageViewModel
    {
        public List<FarmerPickupSlotRowViewModel> Slots { get; set; } = new();

        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int FullSlots { get; set; }
        public int TotalCapacity { get; set; }
        public int TotalBooked { get; set; }
    }

    public class FarmerPickupSlotRowViewModel
    {
        public int PickupSlotId { get; set; }

        public int FarmerMarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string? StallNumber { get; set; }

        public DateTime PickupDate { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int MaximumOrders { get; set; }
        public int BookedOrders { get; set; }

        public int RemainingCapacity =>
            Math.Max(0, MaximumOrders - BookedOrders);

        public bool IsAvailable { get; set; }

        public bool IsFull =>
            BookedOrders >= MaximumOrders;
    }

    public class CreatePickupSlotViewModel
    {
        [Required]
        [Display(Name = "Market")]
        public int FarmerMarketId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Pickup Date")]
        public DateTime PickupDate { get; set; } =
            DateTime.Today.AddDays(1);

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; } =
            new TimeSpan(9, 0, 0);

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; } =
            new TimeSpan(10, 0, 0);

        [Required]
        [Range(1, 500)]
        [Display(Name = "Maximum Orders")]
        public int MaximumOrders { get; set; } = 10;

        [Display(Name = "Available for Customers")]
        public bool IsAvailable { get; set; } = true;

        public List<SelectListItem> Markets { get; set; } = new();
    }

    public class EditPickupSlotViewModel
    {
        public int PickupSlotId { get; set; }

        public string MarketName { get; set; } = "";
        public string? StallNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Pickup Date")]
        public DateTime PickupDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Range(1, 500)]
        [Display(Name = "Maximum Orders")]
        public int MaximumOrders { get; set; }

        public int BookedOrders { get; set; }

        [Display(Name = "Available for Customers")]
        public bool IsAvailable { get; set; }
    }
}
