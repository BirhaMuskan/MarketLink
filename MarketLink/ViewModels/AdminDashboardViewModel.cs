using System;
using System.Collections.Generic;

namespace MarketLink.ViewModels
{
    public class AdminDashboardViewModel
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        // Main KPIs
        public int TotalFarmers { get; set; }
        public int NewFarmersThisMonth { get; set; }
        public int PendingFarmerApprovals { get; set; }

        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }

        public int ActiveMarkets { get; set; }
        public int TotalMarkets { get; set; }

        public int TotalOrders { get; set; }
        public int OrdersThisMonth { get; set; }

        // Secondary KPIs
        public int TotalProducts { get; set; }
        public int PendingOrders { get; set; }
        public int TotalReviews { get; set; }
        public decimal MarketplaceSales { get; set; }

        // Moderation / admin
        public int PendingProductApprovals { get; set; }
        public int HiddenReviews { get; set; }
        public int UnreadAdminNotifications { get; set; }

        // AI / intelligence
        public int UnacknowledgedAnomalies { get; set; }
        public int HighShortageRisks { get; set; }
        public int HighWasteRisks { get; set; }
        public string LatestAiRunStatus { get; set; } = "No runs";
        public string LatestAiModelName { get; set; } = "-";
        public DateTime? LatestAiRunAt { get; set; }

        public List<RecentOrderRow> RecentOrders { get; set; } = new();
        public List<FarmerOverviewRow> Farmers { get; set; } = new();
        public List<CustomerOverviewRow> Customers { get; set; } = new();
        public List<ChartPoint> OrdersLast7Days { get; set; } = new();
        public List<ChartPoint> RevenueLast6Months { get; set; } = new();
        public List<ChartPoint> TopProducts { get; set; } = new();
        public List<IntelligenceAlertRow> IntelligenceAlerts { get; set; } = new();
    }

    public class RecentOrderRow
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public string ProductSummary { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string FarmerName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public DateTime OrderDate { get; set; }
    }

    public class FarmerOverviewRow
    {
        public int FarmerId { get; set; }
        public string BusinessName { get; set; } = "";
        public string MarketName { get; set; } = "No market assigned";
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
    }

    public class CustomerOverviewRow
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = "";
        public int OrderCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public decimal Value { get; set; }
    }

    public class IntelligenceAlertRow
    {
        public string Type { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class AdminSidebarSummaryViewModel
    {
        public int Farmers { get; set; }
        public int Customers { get; set; }
        public int OrdersNeedingAttention { get; set; }
        public int Reviews { get; set; }
        public int UnreadNotifications { get; set; }
    }
}
