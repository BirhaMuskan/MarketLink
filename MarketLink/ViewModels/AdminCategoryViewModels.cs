using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class AdminCategoriesPageViewModel
    {
        public List<AdminCategoryRowViewModel> Categories { get; set; } = new();

        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int InactiveCategories { get; set; }
    }

    public class AdminCategoryRowViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = "";
        public string Description { get; set; } = "";

        public bool IsActive { get; set; }

        public int ProductCount { get; set; }
    }

    public class AdminCategoryFormViewModel
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = "";

        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
