using MarketLink.DTOs;

using MarketLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Farmer")]
    public class FarmerDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // You can later move this to SystemSetting.
        private const decimal LowStockThreshold = 10m;

        public FarmerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard(
            CancellationToken cancellationToken)
        {
            // ---------------------------------------------------------
            // 1. READ LOGGED-IN USER ID FROM JWT
            // ---------------------------------------------------------
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }

            // ---------------------------------------------------------
            // 2. GET APPROVED FARMER PROFILE
            // ---------------------------------------------------------
            var farmer = await _context.farmers
                .AsNoTracking()
                .Include(f => f.User)
                .FirstOrDefaultAsync(
                    f => f.UserId == userId,
                    cancellationToken);

            if (farmer == null)
            {
                return NotFound(new
                {
                    message = "Farmer profile was not found."
                });
            }

            if (!farmer.IsApproved || !farmer.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Farmer account is not approved or active."
                });
            }

            var farmerId = farmer.FarmerId;

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonth = monthStart.AddMonths(1);

            // ---------------------------------------------------------
            // 3. MAIN KPI VALUES
            // ---------------------------------------------------------
            var totalProducts = await _context.farmerproducts
                .AsNoTracking()
                .CountAsync(
                    fp =>
                        fp.FarmerId == farmerId &&
                        fp.IsActive &&
                        fp.IsApproved,
                    cancellationToken);

            // "Pending" means a new order still waiting for the farmer
            // to accept/decline it.
            var pendingOrders = await _context.orders
                .AsNoTracking()
                .CountAsync(
                    o =>
                        o.FarmerId == farmerId &&
                        o.OrderStatus == "Placed",
                    cancellationToken);

            var thisMonthSales = await _context.orders
                .AsNoTracking()
                .Where(o =>
                    o.FarmerId == farmerId &&
                    o.OrderStatus == "Completed" &&
                    o.OrderDate >= monthStart &&
                    o.OrderDate < nextMonth)
                .SumAsync(
                    o => (decimal?)o.TotalAmount,
                    cancellationToken) ?? 0m;

            var ratingData = await _context.reviews
                .AsNoTracking()
                .Where(r =>
                    r.FarmerId == farmerId &&
                    r.IsVisible)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    ReviewCount = g.Count(),
                    AverageRating = g.Average(r => (decimal)r.Rating)
                })
                .FirstOrDefaultAsync(cancellationToken);

            // ---------------------------------------------------------
            // 4. INCOMING / ACTIVE ORDERS
            // ---------------------------------------------------------
            var incomingOrders = await _context.orders
                .AsNoTracking()
                .Where(o =>
                    o.FarmerId == farmerId &&
                    o.OrderStatus != "Completed" &&
                    o.OrderStatus != "Cancelled" &&
                    o.OrderStatus != "Declined")
                .OrderBy(o => o.PickupDate)
                .ThenByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new FarmerDashboardOrderDto
                {
                    OrderId = o.OrderId,
                    OrderNo = o.OrderNo,

                    ItemCount = _context.orderitems
                        .Count(oi => oi.OrderId == o.OrderId),

                    PickupDate = o.PickupDate,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus
                })
                .ToListAsync(cancellationToken);

            // ---------------------------------------------------------
            // 5. BEST SELLING PRODUCTS
            // Completed-order line snapshots are used so historical
            // sales stay accurate even if listing data changes later.
            // ---------------------------------------------------------
            var bestSellingProducts = await _context.orderitems
                .AsNoTracking()
                .Where(oi =>
                    oi.Order != null &&
                    oi.Order.FarmerId == farmerId &&
                    oi.Order.OrderStatus == "Completed")
                .GroupBy(oi => oi.ProductName)
                .Select(g => new FarmerTopProductDto
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync(cancellationToken);

            // ---------------------------------------------------------
            // 6. LATEST INVENTORY PER FARMER PRODUCT
            // Available quantity = Stock - Reserved - Sold
            // ---------------------------------------------------------
            var inventoryRows = await _context.inventories
                .AsNoTracking()
                .Where(i =>
                    i.FarmerProduct != null &&
                    i.FarmerProduct.FarmerId == farmerId &&
                    i.FarmerProduct.IsActive)
                .OrderByDescending(i => i.UpdatedAt)
                .Select(i => new
                {
                    i.InventoryId,
                    i.FarmerProductId,

                    ProductName =
                        i.FarmerProduct != null &&
                        i.FarmerProduct.Product != null
                            ? i.FarmerProduct.Product.ProductName
                            : "Product",

                    UnitName =
                        i.FarmerProduct != null &&
                        i.FarmerProduct.UnitOfMeasure != null
                            ? i.FarmerProduct.UnitOfMeasure.UnitCode
                            : "",

                    AvailableQuantity =
                        i.StockQuantity -
                        i.ReservedQuantity -
                        i.SoldQuantity,

                    i.IsSoldOut,
                    i.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            var stockOverview = inventoryRows
                .GroupBy(x => x.FarmerProductId)
                .Select(g => g
                    .OrderByDescending(x => x.UpdatedAt)
                    .First())
                .OrderBy(x => x.AvailableQuantity)
                .Take(6)
                .Select(x => new FarmerStockDto
                {
                    InventoryId = x.InventoryId,
                    FarmerProductId = x.FarmerProductId,
                    ProductName = x.ProductName,
                    UnitName = x.UnitName,
                    AvailableQuantity = x.AvailableQuantity,
                    IsLowStock =
                        x.AvailableQuantity <= LowStockThreshold,
                    IsSoldOut =
                        x.IsSoldOut ||
                        x.AvailableQuantity <= 0
                })
                .ToList();

            // ---------------------------------------------------------
            // 7. LATEST CUSTOMER REVIEW
            // ---------------------------------------------------------
            var latestReview = await _context.reviews
                .AsNoTracking()
                .Where(r =>
                    r.FarmerId == farmerId &&
                    r.IsVisible)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => new FarmerLatestReviewDto
                {
                    ReviewId = r.ReviewId,

                    CustomerName =
                        r.Customer != null &&
                        r.Customer.User != null
                            ? r.Customer.User.FullName
                            : "Customer",

                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            // ---------------------------------------------------------
            // 8. TODAY'S MARKET STATUS
            // ---------------------------------------------------------
            var todayName = now.DayOfWeek.ToString();

            var marketStatus = await _context.farmermarketdays
                .AsNoTracking()
                .Where(fmd =>
                    fmd.IsActive &&
                    fmd.FarmerMarket != null &&
                    fmd.FarmerMarket.IsActive &&
                    fmd.FarmerMarket.FarmerId == farmerId &&
                    fmd.MarketDay != null &&
                    fmd.MarketDay.IsActive &&
                    fmd.MarketDay.DayName == todayName)
                .Select(fmd => new FarmerMarketStatusDto
                {
                    FarmerMarketId = fmd.FarmerMarketId,

                    MarketName =
                        fmd.FarmerMarket != null &&
                        fmd.FarmerMarket.Market != null
                            ? fmd.FarmerMarket.Market.MarketName
                            : "Market",

                    DayName =
                        fmd.MarketDay != null
                            ? fmd.MarketDay.DayName
                            : todayName,

                    OpeningTime =
                        fmd.MarketDay != null
                            ? fmd.MarketDay.OpeningTime
                            : TimeSpan.Zero,

                    ClosingTime =
                        fmd.MarketDay != null
                            ? fmd.MarketDay.ClosingTime
                            : TimeSpan.Zero,

                    Status = "Active"
                })
                .FirstOrDefaultAsync(cancellationToken);

            // If the farmer has no market today, show the first active
            // assigned market/day rather than leaving the dashboard blank.
            if (marketStatus == null)
            {
                marketStatus = await _context.farmermarketdays
                    .AsNoTracking()
                    .Where(fmd =>
                        fmd.IsActive &&
                        fmd.FarmerMarket != null &&
                        fmd.FarmerMarket.IsActive &&
                        fmd.FarmerMarket.FarmerId == farmerId &&
                        fmd.MarketDay != null &&
                        fmd.MarketDay.IsActive)
                    .OrderBy(fmd => fmd.MarketDayId)
                    .Select(fmd => new FarmerMarketStatusDto
                    {
                        FarmerMarketId = fmd.FarmerMarketId,

                        MarketName =
                            fmd.FarmerMarket != null &&
                            fmd.FarmerMarket.Market != null
                                ? fmd.FarmerMarket.Market.MarketName
                                : "Market",

                        DayName =
                            fmd.MarketDay != null
                                ? fmd.MarketDay.DayName
                                : "",

                        OpeningTime =
                            fmd.MarketDay != null
                                ? fmd.MarketDay.OpeningTime
                                : TimeSpan.Zero,

                        ClosingTime =
                            fmd.MarketDay != null
                                ? fmd.MarketDay.ClosingTime
                                : TimeSpan.Zero,

                        Status = "Scheduled"
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // ---------------------------------------------------------
            // 9. FINAL RESPONSE
            // Property names match your current Dashboard.cshtml JS.
            // ---------------------------------------------------------
            var dashboard = new FarmerDashboardDto
            {
                FarmerId = farmerId,
                BusinessName = farmer.BusinessName,

                TotalProducts = totalProducts,
                PendingOrders = pendingOrders,
                ThisMonthSales = thisMonthSales,

                AverageRating = ratingData?.AverageRating ?? 0m,
                ReviewCount = ratingData?.ReviewCount ?? 0,

                IncomingOrders = incomingOrders,
                BestSellingProducts = bestSellingProducts,
                StockOverview = stockOverview,
                LatestReview = latestReview,
                MarketStatus = marketStatus
            };

            return Ok(dashboard);
        }
    }
}
