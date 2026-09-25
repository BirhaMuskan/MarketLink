using System.ComponentModel.DataAnnotations;

namespace MarketLink.DTOs
{
    public class RegisterFarmerDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Phone { get; set; } = "";

        public string Address { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string BusinessName { get; set; } = "";

        public string Description { get; set; } = "";
    }
}