using System.ComponentModel.DataAnnotations;

namespace MarketLink.ViewModels
{
    public class AdminAnnouncementsPageViewModel
    {
        public List<AdminAnnouncementRowViewModel> Announcements { get; set; } = new();

        public int Total { get; set; }
        public int Active { get; set; }
        public int Scheduled { get; set; }
        public int Expired { get; set; }

        public string Search { get; set; } = "";
        public string Audience { get; set; } = "ALL";
    }

    public class AdminAnnouncementRowViewModel
    {
        public int AnnouncementId { get; set; }

        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Audience { get; set; } = "ALL";
        public string CreatedByName { get; set; } = "";

        public DateTime PublishFrom { get; set; }
        public DateTime? PublishUntil { get; set; }

        public bool IsActive { get; set; }

        public string Status { get; set; } = "";
    }

    public class AdminAnnouncementEditViewModel
    {
        public int AnnouncementId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Audience { get; set; } = "ALL";

        [Required]
        [Display(Name = "Publish From")]
        public DateTime PublishFrom { get; set; } = DateTime.Now;

        [Display(Name = "Publish Until")]
        public DateTime? PublishUntil { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class AnnouncementBannerViewModel
    {
        public List<AnnouncementBannerItemViewModel> Items { get; set; } = new();
    }

    public class AnnouncementBannerItemViewModel
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Audience { get; set; } = "";
    }
}
