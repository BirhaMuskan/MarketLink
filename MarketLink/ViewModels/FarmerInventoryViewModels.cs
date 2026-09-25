using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class FarmerInventoryPageViewModel
    {
        public List<FarmerInventoryRowViewModel> Rows { get; set; } = new();

        public int TotalRows { get; set; }
        public int AvailableRows { get; set; }
        public int SoldOutRows { get; set; }

        public decimal TotalStock { get; set; }
        public decimal TotalAvailable { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalSold { get; set; }
    }

    public class FarmerInventoryRowViewModel
    {
        public int InventoryId { get; set; }
        public int FarmerProductId { get; set; }
        public int FarmerMarketId { get; set; }

        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public string MarketName { get; set; } = "";

        public DateTime InventoryDate { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal SoldQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }

        public bool IsSoldOut { get; set; }
        public bool IsAvailable { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class EditFarmerInventoryViewModel
    {
        public int InventoryId { get; set; }

        public string ProductName { get; set; } = "";
        public string MarketName { get; set; } = "";
        public string UnitName { get; set; } = "";

        [Required]
        [Range(0, 999999999)]
        [Display(Name = "Stock Quantity")]
        public decimal StockQuantity { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Available for Customers")]
        public bool IsAvailable { get; set; }

        [StringLength(500)]
        [Display(Name = "Change Remarks")]
        public string? Remarks { get; set; }
    }

    public class FarmerWeeklyStockPageViewModel
    {
        public List<FarmerWeeklyStockRowViewModel> Rows { get; set; } = new();

        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public decimal TotalDefaultQuantity { get; set; }
    }

    public class FarmerWeeklyStockRowViewModel
    {
        public int? RecurringStockTemplateId { get; set; }

        public int FarmerProductId { get; set; }
        public int FarmerMarketDayId { get; set; }

        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public string MarketName { get; set; } = "";
        public string DayName { get; set; } = "";

        public TimeSpan PickupStartTime { get; set; }
        public TimeSpan PickupEndTime { get; set; }

        public decimal DefaultQuantity { get; set; }

        public bool IsConfigured { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveWeeklyStockViewModel
    {
        [Required]
        public int FarmerProductId { get; set; }

        [Required]
        public int FarmerMarketDayId { get; set; }

        [Required]
        [Range(0, 999999999)]
        [Display(Name = "Default Weekly Quantity")]
        public decimal DefaultQuantity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
