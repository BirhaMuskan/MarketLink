using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MarketLink.ViewModels
{
    public class FarmerProfileMapViewModel
    {
        public int FarmerId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string Phone { get; set; } = "";

        [Required]
        [StringLength(150)]
        [Display(Name = "Business / Stall Name")]
        public string BusinessName { get; set; } = "";

        public string Description { get; set; } = "";

        [Display(Name = "Business Address")]
        public string Address { get; set; } = "";

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImageFile { get; set; }

        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
    }

    public class FarmerTopbarViewModel
    {
        public string FullName { get; set; } = "Farmer";
        public string BusinessName { get; set; } = "Farmer";
        public string? ProfileImageUrl { get; set; }

        public string Initial =>
            string.IsNullOrWhiteSpace(FullName)
                ? "F"
                : FullName.Trim()[0].ToString().ToUpperInvariant();
    }
}
