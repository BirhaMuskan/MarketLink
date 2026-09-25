using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class FarmerProductsPageViewModel
    {
        public List<FarmerProductRowViewModel> Products { get; set; } = new();

        public int TotalProducts { get; set; }
        public int AvailableProducts { get; set; }
        public int SoldOutProducts { get; set; }
        public decimal TotalAvailableQuantity { get; set; }
    }

    public class FarmerProductRowViewModel
    {
        public int FarmerProductId { get; set; }

        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string UnitName { get; set; } = "";

        public decimal Price { get; set; }
        public decimal AvailableQuantity { get; set; }

        public string? FarmerDescription { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; }
        public bool IsActive { get; set; }
        public bool IsSoldOut { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateFarmerProductViewModel
    {
        [Required, StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = "";

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Unit")]
        public int UnitOfMeasureId { get; set; }

        [Required]
        [Display(Name = "Market")]
        public int FarmerMarketId { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 999999999)]
        [Display(Name = "Quantity Available")]
        public decimal StockQuantity { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Available for Customers")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Units { get; set; } = new();
        public List<SelectListItem> Markets { get; set; } = new();
    }

    public class EditFarmerProductViewModel
    {
        public int FarmerProductId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = "";

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int UnitOfMeasureId { get; set; }

        [Required]
        public int FarmerMarketId { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 999999999)]
        [Display(Name = "Quantity Available")]
        public decimal StockQuantity { get; set; }

        public string? Description { get; set; }

        public bool IsAvailable { get; set; }

        public string? ExistingImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Units { get; set; } = new();
        public List<SelectListItem> Markets { get; set; } = new();
    }
}
