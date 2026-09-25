namespace MarketLink.ViewModels
{
    public class FarmerApprovalPageViewModel
    {
        public List<FarmerApprovalRowViewModel> PendingFarmers { get; set; } = new();

        public int PendingCount => PendingFarmers.Count;
    }

    public class FarmerApprovalRowViewModel
    {
        public int FarmerId { get; set; }
        public int UserId { get; set; }

        public string FullName { get; set; } = "";
        public string BusinessName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Description { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public bool IsApproved { get; set; }
        public bool UserIsActive { get; set; }
    }
}
