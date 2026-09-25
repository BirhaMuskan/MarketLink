using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ApplicationDbContext _db;

        public AdminDashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonth = monthStart.AddMonths(1);
            var sevenDaysAgo = now.Date.AddDays(-6);

            var model = new AdminDashboardViewModel
            {
                GeneratedAt = now,

                TotalFarmers = await _db.farmers
                .AsNoTracking()
                .CountAsync(
                    x => !x.IsApproved,
                    cancellationToken
                ),

                NewFarmersThisMonth = await _db.farmers.AsNoTracking()
                    .CountAsync(x => x.CreatedAt >= monthStart && x.CreatedAt < nextMonth,
                        cancellationToken),

                PendingFarmerApprovals = await _db.farmers.AsNoTracking()
                    .CountAsync(x => !x.IsApproved, cancellationToken),

                TotalCustomers = await _db.customers.AsNoTracking()
                    .CountAsync(cancellationToken),

                NewCustomersThisMonth = await _db.customers.AsNoTracking()
                    .CountAsync(x => x.CreatedAt >= monthStart && x.CreatedAt < nextMonth,
                        cancellationToken),

                ActiveMarkets = await _db.markets.AsNoTracking()
                    .CountAsync(x => x.IsActive, cancellationToken),

                TotalMarkets = await _db.markets.AsNoTracking()
                    .CountAsync(cancellationToken),

                TotalOrders = await _db.orders.AsNoTracking()
                    .CountAsync(cancellationToken),

                OrdersThisMonth = await _db.orders.AsNoTracking()
                    .CountAsync(x => x.OrderDate >= monthStart && x.OrderDate < nextMonth,
                        cancellationToken),

                TotalProducts = await _db.products.AsNoTracking()
                    .CountAsync(x => x.IsActive, cancellationToken),

                PendingOrders = await _db.orders.AsNoTracking()
                    .CountAsync(x => x.OrderStatus != "Completed" &&
                                     x.OrderStatus != "Cancelled",
                        cancellationToken),

                TotalReviews = await _db.reviews.AsNoTracking()
                    .CountAsync(cancellationToken),

                MarketplaceSales = await _db.orders.AsNoTracking()
                    .Where(x => x.OrderStatus == "Completed")
                    .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m,

                PendingProductApprovals = await _db.farmerproducts.AsNoTracking()
                    .CountAsync(x => !x.IsApproved, cancellationToken),

                HiddenReviews = await _db.reviews.AsNoTracking()
                    .CountAsync(x => !x.IsVisible, cancellationToken),

                UnreadAdminNotifications = await _db.notifications.AsNoTracking()
                    .CountAsync(x => !x.IsRead &&
                                     x.User != null &&
                                     x.User.Role != null &&
                                     x.User.Role.RoleName == "Admin",
                        cancellationToken),

                UnacknowledgedAnomalies = await _db.anomalydetections.AsNoTracking()
                    .CountAsync(x => !x.IsAcknowledged, cancellationToken),

                HighShortageRisks = await _db.demandpredictions.AsNoTracking()
                    .CountAsync(x => x.ShortageRisk == "High" ||
                                     x.ShortageRisk == "Critical",
                        cancellationToken),

                HighWasteRisks = await _db.wasteriskpredictions.AsNoTracking()
                    .CountAsync(x => x.RiskLevel == "High" ||
                                     x.RiskLevel == "Critical",
                        cancellationToken)
            };

            model.RecentOrders = await _db.orders.AsNoTracking()
                .OrderByDescending(x => x.OrderDate)
                .Take(6)
                .Select(x => new RecentOrderRow
                {
                    OrderId = x.OrderId,
                    OrderNo = x.OrderNo,
                    ProductSummary = _db.orderitems
                        .Where(i => i.OrderId == x.OrderId)
                        .OrderBy(i => i.OrderItemId)
                        .Select(i => i.ProductName)
                        .FirstOrDefault() ?? "Order items",
                    CustomerName = x.Customer != null && x.Customer.User != null
                        ? x.Customer.User.FullName
                        : "Customer",
                    FarmerName = x.Farmer != null
                        ? x.Farmer.BusinessName
                        : "Farmer",
                    TotalAmount = x.TotalAmount,
                    Status = x.OrderStatus,
                    OrderDate = x.OrderDate
                })
                .ToListAsync(cancellationToken);

            model.Farmers = await _db.farmers.AsNoTracking()
                .OrderBy(x => x.IsApproved)
                .ThenByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new FarmerOverviewRow
                {
                    FarmerId = x.FarmerId,
                    BusinessName = x.BusinessName,
                    IsApproved = x.IsApproved,
                    IsActive = x.IsActive,
                    MarketName = _db.farmermarkets
                        .Where(fm => fm.FarmerId == x.FarmerId && fm.IsActive)
                        .OrderBy(fm => fm.FarmerMarketId)
                        .Select(fm => fm.Market != null ? fm.Market.MarketName : null)
                        .FirstOrDefault() ?? "No market assigned"
                })
                .ToListAsync(cancellationToken);

            model.Customers = await _db.customers.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new CustomerOverviewRow
                {
                    CustomerId = x.CustomerId,
                    FullName = x.User != null ? x.User.FullName : "Customer",
                    CreatedAt = x.CreatedAt,
                    OrderCount = _db.orders.Count(o => o.CustomerId == x.CustomerId)
                })
                .ToListAsync(cancellationToken);

            var dailyOrders = await _db.orders.AsNoTracking()
                .Where(x => x.OrderDate >= sevenDaysAgo)
                .GroupBy(x => x.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            model.OrdersLast7Days = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = sevenDaysAgo.AddDays(offset);
                    var row = dailyOrders.FirstOrDefault(x => x.Date == date);

                    return new ChartPoint
                    {
                        Label = date.ToString("ddd"),
                        Value = row?.Count ?? 0
                    };
                })
                .ToList();

            var revenueStart = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

            var revenueRaw = await _db.orders.AsNoTracking()
                .Where(x => x.OrderStatus == "Completed" &&
                            x.OrderDate >= revenueStart)
                .GroupBy(x => new
                {
                    x.OrderDate.Year,
                    x.OrderDate.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            model.RevenueLast6Months = Enumerable.Range(0, 6)
                .Select(offset =>
                {
                    var month = revenueStart.AddMonths(offset);

                    var row = revenueRaw.FirstOrDefault(x =>
                        x.Year == month.Year &&
                        x.Month == month.Month);

                    return new ChartPoint
                    {
                        Label = month.ToString("MMM"),
                        Value = row?.Amount ?? 0m
                    };
                })
                .ToList();

            model.TopProducts = await _db.orderitems.AsNoTracking()
                .GroupBy(x => x.ProductName)
                .Select(g => new ChartPoint
                {
                    Label = g.Key,
                    Value = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToListAsync(cancellationToken);

            var latestAiRun = await _db.aimodelruns.AsNoTracking()
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new
                {
                    x.ModelName,
                    x.ModelVersion,
                    x.Status,
                    x.StartedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (latestAiRun != null)
            {
                model.LatestAiModelName = string.IsNullOrWhiteSpace(latestAiRun.ModelVersion)
                    ? latestAiRun.ModelName
                    : $"{latestAiRun.ModelName} v{latestAiRun.ModelVersion}";

                model.LatestAiRunStatus = latestAiRun.Status;
                model.LatestAiRunAt = latestAiRun.StartedAt;
            }

            var anomalyAlerts = await _db.anomalydetections.AsNoTracking()
                .Where(x => !x.IsAcknowledged)
                .OrderByDescending(x => x.DetectedAt)
                .Take(4)
                .Select(x => new IntelligenceAlertRow
                {
                    Type = "Demand anomaly",
                    Severity = x.Severity,
                    Message = x.Message,
                    CreatedAt = x.DetectedAt
                })
                .ToListAsync(cancellationToken);

            var wasteAlerts = await _db.wasteriskpredictions.AsNoTracking()
                .Where(x => x.RiskLevel == "High" ||
                            x.RiskLevel == "Critical")
                .OrderByDescending(x => x.CreatedAt)
                .Take(4)
                .Select(x => new IntelligenceAlertRow
                {
                    Type = "Waste risk",
                    Severity = x.RiskLevel,
                    Message = x.Recommendation,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            model.IntelligenceAlerts = anomalyAlerts
                .Concat(wasteAlerts)
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToList();

            return model;
        }

        public async Task<AdminSidebarSummaryViewModel> GetSidebarSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            return new AdminSidebarSummaryViewModel
            {
                Farmers = await _db.farmers.AsNoTracking()
                    .CountAsync(cancellationToken),

                Customers = await _db.customers.AsNoTracking()
                    .CountAsync(cancellationToken),

                OrdersNeedingAttention = await _db.orders.AsNoTracking()
                    .CountAsync(x => x.OrderStatus != "Completed" &&
                                     x.OrderStatus != "Cancelled",
                        cancellationToken),

                Reviews = await _db.reviews.AsNoTracking()
                    .CountAsync(cancellationToken),

                UnreadNotifications = await _db.notifications.AsNoTracking()
                    .CountAsync(x => !x.IsRead, cancellationToken)
            };
        }
    }
}
