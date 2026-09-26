using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class CustomerCartPageViewModel
    {
        public int CartId { get; set; }
        public int FarmerId { get; set; }
        public int FarmerMarketId { get; set; }

        public string FarmerName { get; set; } = "";
        public string MarketName { get; set; } = "";
        public string MarketAddress { get; set; } = "";
        public string? StallNumber { get; set; }

        public List<CustomerCartItemViewModel> Items { get; set; } = new();

        public decimal EstimatedTotal =>
            Items.Sum(x => x.LineTotal);
    }

    public class CustomerCartItemViewModel
    {
        public int CartItemId { get; set; }
        public int FarmerProductId { get; set; }

        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public string? ImageUrl { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal AvailableQuantity { get; set; }

        public decimal LineTotal =>
            Quantity * UnitPrice;
    }

    public class AddToCartViewModel
    {
        public int FarmerProductId { get; set; }
        public int FarmerMarketId { get; set; }

        [Range(typeof(decimal), "0.001", "999999")]
        public decimal Quantity { get; set; } = 1;

        public string? ReturnUrl { get; set; }
    }

    public class UpdateCartItemViewModel
    {
        public int CartItemId { get; set; }

        [Range(typeof(decimal), "0.001", "999999")]
        public decimal Quantity { get; set; }
    }

    public class CustomerCheckoutViewModel
    {
        public int CartId { get; set; }

        [Required]
        [Display(Name = "Pickup Slot")]
        public int? PickupSlotId { get; set; }

        [StringLength(1000)]
        [Display(Name = "Order Notes")]
        public string? CustomerNotes { get; set; }

        public string FarmerName { get; set; } = "";
        public string MarketName { get; set; } = "";
        public string MarketAddress { get; set; } = "";

        public List<CustomerCheckoutItemViewModel> Items { get; set; } = new();
        public List<CustomerPickupSlotOptionViewModel> PickupSlots { get; set; } = new();

        public decimal EstimatedTotal =>
            Items.Sum(x => x.LineTotal);
    }

    public class CustomerCheckoutItemViewModel
    {
        public int FarmerProductId { get; set; }

        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal LineTotal =>
            Quantity * UnitPrice;
    }

    public class CustomerPickupSlotOptionViewModel
    {
        public int PickupSlotId { get; set; }

        public DateTime PickupDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int MaximumOrders { get; set; }
        public int BookedOrders { get; set; }

        public int RemainingCapacity =>
            Math.Max(0, MaximumOrders - BookedOrders);

        public string DisplayText =>
            $"{PickupDate:ddd, dd MMM yyyy} | " +
            $"{DateTime.Today.Add(StartTime):hh:mm tt} - " +
            $"{DateTime.Today.Add(EndTime):hh:mm tt} | " +
            $"{RemainingCapacity} slot(s) left";
    }

    public class CustomerOrderSuccessViewModel
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string MarketName { get; set; } = "";
        public DateTime PickupDate { get; set; }
        public string PickupTimeText { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }
}
