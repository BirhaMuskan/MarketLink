using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class CustomerProfileViewModel
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string Phone { get; set; } = "";

        public string Address { get; set; } = "";

        public DateTime AccountCreatedAt { get; set; }
        public DateTime CustomerSince { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; }

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int FavoriteProducts { get; set; }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                    return "C";

                var parts = FullName.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 1)
                    return parts[0][0]
                        .ToString()
                        .ToUpperInvariant();

                return string.Concat(
                    parts[0][0],
                    parts[^1][0])
                    .ToUpperInvariant();
            }
        }
    }

    public class ChangeCustomerPasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "New password and confirmation do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}
