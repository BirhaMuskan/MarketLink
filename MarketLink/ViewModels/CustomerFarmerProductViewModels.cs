using Microsoft.AspNetCore.Mvc.Rendering;

namespace MarketLink.ViewModels
{
    public class CustomerFarmerDetailsViewModel
    {
        public int FarmerId { get; set; }
        public string BusinessName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";
        public string? ProfileImageUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsFavorite { get; set; }
        public List<CustomerFarmerMarketViewModel> Markets { get; set; } = new();
        public List<CustomerProductCardViewModel> Products { get; set; } = new();
    }

    public class CustomerFarmerMarketViewModel
    {
        public int FarmerMarketId { get; set; }
        public int MarketId { get; set; }
        public string MarketName { get; set; } = "";
        public string Address { get; set; } = "";
        public string? StallNumber { get; set; }
        public string? PickupInstructions { get; set; }
        public string OperatingDaysText { get; set; } = "";
    }

    public class CustomerProductsPageViewModel
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? MarketId { get; set; }
        public int? FarmerId { get; set; }
        public bool AvailableOnly { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Markets { get; set; } = new();
        public List<SelectListItem> Farmers { get; set; } = new();

        public List<CustomerProductCardViewModel> Products { get; set; } = new();

        public int TotalProducts { get; set; }
        public int AvailableProducts { get; set; }
        public int FavoriteProducts { get; set; }
    }

    public class CustomerProductCardViewModel
    {
        public int FarmerProductId { get; set; }
        public int FarmerId { get; set; }
        public int ProductId { get; set; }
        public int CategoryId { get; set; }

        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public string Description { get; set; } = "";
        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }
        public decimal AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string MarketSummary { get; set; } = "";
        public bool IsFavorite { get; set; }
    }

    public class CustomerProductDetailsViewModel
    {
        public int FarmerProductId { get; set; }
        public int FarmerId { get; set; }

        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public string Description { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsFavorite { get; set; }

        public List<string> Images { get; set; } = new();
        public List<CustomerProductInventoryViewModel> Inventory { get; set; } = new();
    }

    public class CustomerProductInventoryViewModel
    {
        public int InventoryId { get; set; }
        public int FarmerMarketId { get; set; }
        public int MarketId { get; set; }

        public string MarketName { get; set; } = "";
        public string MarketAddress { get; set; } = "";

        public DateTime InventoryDate { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal SoldQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }

        public bool IsAvailable { get; set; }
        public bool IsSoldOut { get; set; }
    }
}
