using MarketLink.DTOs.Farmer;
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

        public FarmerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // GET FARMER DASHBOARD
        // GET: /api/FarmerDashboard
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            // --------------------------------------------------------
            // 1. GET LOGGED-IN USER ID
            // --------------------------------------------------------

            var userIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user identity."
                });
            }


            // --------------------------------------------------------
            // 2. FIND FARMER
            // --------------------------------------------------------

            var farmer = await _context.farmers
                .Include(f => f.User)
                .FirstOrDefaultAsync(f =>
                    f.UserId == userId &&
                    f.IsApproved &&
                    f.IsActive
                );

            if (farmer == null)
            {
                return NotFound(new
                {
                    message = "Farmer profile not found or not approved."
                });
            }


            // --------------------------------------------------------
            // 3. CREATE DASHBOARD OBJECT
            // --------------------------------------------------------

            var dashboard = new FarmerDashboardDto
            {
                FarmerId = farmer.FarmerId,

                BusinessName = farmer.BusinessName,

                FullName = farmer.User?.FullName ?? "",

                ProfileImageUrl = farmer.ProfileImageUrl
            };


            // ========================================================
            // 4. TOTAL PRODUCTS
            // ========================================================

            dashboard.TotalProducts = await _context.farmerproducts
                .CountAsync(fp =>
                    fp.FarmerId == farmer.FarmerId &&
                    fp.IsActive
                );


            // ========================================================
            // 5. PENDING ORDERS
            // ========================================================

            dashboard.PendingOrders = await _context.orders
                .CountAsync(o =>
                    o.FarmerId == farmer.FarmerId &&
                    (
                        o.OrderStatus == "Placed" ||
                        o.OrderStatus == "Pending"
                    )
                );


            // ========================================================
            // 6. THIS MONTH SALES
            // ========================================================

            var now = DateTime.Now;

            dashboard.ThisMonthSales =
                await _context.orders
                    .Where(o =>
                        o.FarmerId == farmer.FarmerId &&

                        o.OrderDate.Year == now.Year &&

                        o.OrderDate.Month == now.Month &&

                        o.OrderStatus != "Cancelled"
                    )
                    .SumAsync(o => (decimal?)o.TotalAmount)
                    ?? 0;


            // ========================================================
            // 7. CUSTOMER REVIEWS / RATING
            // ========================================================

            var reviews = await _context.reviews
                .Where(r =>
                    r.FarmerId == farmer.FarmerId &&
                    r.IsVisible
                )
                .ToListAsync();


            dashboard.ReviewCount = reviews.Count;


            dashboard.AverageRating = reviews.Count == 0
                ? 0
                : Math.Round(
                    (decimal)reviews.Average(r => r.Rating),
                    1
                );


            // ========================================================
            // 8. INCOMING ORDERS
            // ========================================================

            var today = DateTime.Today;

            dashboard.IncomingOrders =
                await _context.orders
                    .Where(o =>
                        o.FarmerId == farmer.FarmerId &&

                        o.PickupDate >= today &&

                        o.OrderStatus != "Completed" &&

                        o.OrderStatus != "Cancelled"
                    )
                    .OrderBy(o => o.PickupDate)
                    .ThenByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new DashboardOrderDto
                    {
                        OrderId = o.OrderId,

                        OrderNo = o.OrderNo,

                        ItemCount = _context.orderitems
                            .Count(oi =>
                                oi.OrderId == o.OrderId
                            ),

                        PickupDate = o.PickupDate,

                        TotalAmount = o.TotalAmount,

                        OrderStatus = o.OrderStatus,

                        CustomerName =
                            o.Customer != null &&
                            o.Customer.User != null
                                ? o.Customer.User.FullName
                                : "Customer"
                    })
                    .ToListAsync();


            // ========================================================
            // 9. BEST SELLING PRODUCTS
            // ========================================================

            dashboard.BestSellingProducts =
                await _context.orderitems
                    .Where(oi =>
                        oi.FarmerProduct != null &&

                        oi.FarmerProduct.FarmerId ==
                            farmer.FarmerId &&

                        oi.Order != null &&

                        oi.Order.OrderStatus != "Cancelled"
                    )
                    .GroupBy(oi => new
                    {
                        oi.FarmerProductId,

                        oi.ProductName
                    })
                    .Select(g => new BestSellingProductDto
                    {
                        FarmerProductId =
                            g.Key.FarmerProductId,

                        ProductName =
                            g.Key.ProductName,

                        QuantitySold =
                            g.Sum(x => x.Quantity),

                        Revenue =
                            g.Sum(x => x.TotalPrice)
                    })
                    .OrderByDescending(x =>
                        x.QuantitySold
                    )
                    .Take(5)
                    .ToListAsync();


            // ========================================================
            // 10. STOCK OVERVIEW
            // ========================================================

            dashboard.StockOverview =
                await _context.inventories
                    .Where(i =>
                        i.FarmerProduct != null &&

                        i.FarmerProduct.FarmerId ==
                            farmer.FarmerId &&

                        i.InventoryDate.Date == today &&

                        i.IsAvailable
                    )
                    .GroupBy(i => new
                    {
                        i.FarmerProductId,

                        ProductName =
                            i.FarmerProduct!
                                .Product!
                                .ProductName,

                        UnitName =
                            i.FarmerProduct!
                                .UnitOfMeasure!
                                .UnitName
                    })
                    .Select(g => new StockOverviewDto
                    {
                        FarmerProductId =
                            g.Key.FarmerProductId,

                        ProductName =
                            g.Key.ProductName,

                        UnitName =
                            g.Key.UnitName,

                        AvailableQuantity =
                            g.Sum(x => x.AvailableQuantity)
                    })
                    .ToListAsync();


            // --------------------------------------------------------
            // LOW STOCK
            // --------------------------------------------------------

            foreach (var stock in dashboard.StockOverview)
            {
                stock.IsLowStock =
                    stock.AvailableQuantity <= 10;
            }


            // ========================================================
            // 11. LATEST REVIEW
            // ========================================================

            dashboard.LatestReview =
                await _context.reviews
                    .Where(r =>
                        r.FarmerId == farmer.FarmerId &&
                        r.IsVisible
                    )
                    .OrderByDescending(r =>
                        r.ReviewDate
                    )
                    .Select(r => new LatestReviewDto
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
                    .FirstOrDefaultAsync();


            // ========================================================
            // 12. MARKET STATUS
            // ========================================================

            var dayName = now.DayOfWeek.ToString();


            var market =
                await _context.farmermarketdays
                    .Where(fmd =>
                        fmd.IsActive &&

                        fmd.FarmerMarket != null &&

                        fmd.FarmerMarket.FarmerId ==
                            farmer.FarmerId &&

                        fmd.FarmerMarket.IsActive &&

                        fmd.MarketDay != null &&

                        fmd.MarketDay.IsActive &&

                        fmd.MarketDay.DayName ==
                            dayName
                    )
                    .Select(fmd => new
                    {
                        FarmerMarketId =
                            fmd.FarmerMarketId,

                        StallNumber =
                            fmd.FarmerMarket!.StallNumber,

                        MarketName =
                            fmd.FarmerMarket.Market != null
                                ? fmd.FarmerMarket.Market.MarketName
                                : "",

                        DayName =
                            fmd.MarketDay!.DayName,

                        OpeningTime =
                            fmd.MarketDay.OpeningTime,

                        ClosingTime =
                            fmd.MarketDay.ClosingTime
                    })
                    .FirstOrDefaultAsync();


            if (market != null)
            {
                string status;


                if (now.TimeOfDay < market.OpeningTime)
                {
                    status = "Upcoming";
                }
                else if (now.TimeOfDay <= market.ClosingTime)
                {
                    status = "Active";
                }
                else
                {
                    status = "Closed";
                }


                dashboard.MarketStatus =
                    new MarketStatusDto
                    {
                        FarmerMarketId =
                            market.FarmerMarketId,

                        MarketName =
                            market.MarketName,

                        DayName =
                            market.DayName,

                        OpeningTime =
                            market.OpeningTime,

                        ClosingTime =
                            market.ClosingTime,

                        Status =
                            status,

                        StallNumber =
                            market.StallNumber
                    };
            }


            // ========================================================
            // 13. RETURN DASHBOARD
            // ========================================================

            return Ok(dashboard);
        }
    }
}