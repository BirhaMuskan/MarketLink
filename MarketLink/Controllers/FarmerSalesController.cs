using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerSalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerSalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var completedOrders = await _context.orders
                .AsNoTracking()
                .Where(o =>
                    o.FarmerId == farmer.FarmerId &&
                    o.OrderStatus == "Completed")
                .OrderByDescending(o => o.CompletedAt ?? o.OrderDate)
                .ToListAsync(cancellationToken);

            var orderIds = completedOrders
                .Select(o => o.OrderId)
                .ToList();

            var customerIds = completedOrders
                .Select(o => o.CustomerId)
                .Distinct()
                .ToList();

            var farmerMarketIds = completedOrders
                .Select(o => o.FarmerMarketId)
                .Distinct()
                .ToList();

            var orderItems = await _context.orderitems
                .AsNoTracking()
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync(cancellationToken);

            var customers = await _context.customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync(cancellationToken);

            var userIds = customers
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var users = await _context.users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, cancellationToken);

            var customerMap = customers
                .ToDictionary(c => c.CustomerId);

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => farmerMarketIds.Contains(fm.FarmerMarketId))
                .ToListAsync(cancellationToken);

            var farmerMarketMap = farmerMarkets
                .ToDictionary(fm => fm.FarmerMarketId);

            var marketIds = farmerMarkets
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var bestSelling = orderItems
                .GroupBy(x => new
                {
                    x.ProductName,
                    x.UnitName
                })
                .Select(g => new FarmerSalesProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    UnitName = g.Key.UnitName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .ThenByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            var recent = completedOrders
                .Take(25)
                .Select(o =>
                {
                    customerMap.TryGetValue(o.CustomerId, out var customer);

                    User? user = null;

                    if (customer != null)
                    {
                        users.TryGetValue(customer.UserId, out user);
                    }

                    farmerMarketMap.TryGetValue(
                        o.FarmerMarketId,
                        out var farmerMarket);

                    Market? market = null;

                    if (farmerMarket != null)
                    {
                        markets.TryGetValue(
                            farmerMarket.MarketId,
                            out market);
                    }

                    return new FarmerSalesOrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderNo = o.OrderNo,
                        CustomerName =
                            user?.FullName ?? "Customer",
                        MarketName =
                            market?.MarketName ?? "Market",
                        OrderDate = o.OrderDate,
                        CompletedAt = o.CompletedAt,
                        TotalAmount = o.TotalAmount
                    };
                })
                .ToList();

            var revenue =
                completedOrders.Sum(o => o.TotalAmount);

            return View(new FarmerSalesPageViewModel
            {
                CompletedRevenue = revenue,
                CompletedOrders = completedOrders.Count,
                AverageOrderValue =
                    completedOrders.Count == 0
                        ? 0
                        : revenue / completedOrders.Count,

                BestSellingProducts = bestSelling,
                RecentCompletedOrders = recent
            });
        }

        private async Task<Farmer?> GetCurrentFarmerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f =>
                        f.UserId == userId &&
                        f.IsApproved &&
                        f.IsActive,
                    cancellationToken);
        }
    }
}
