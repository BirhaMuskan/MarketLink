using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerDashboardController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
            {
                return Forbid();
            }

            var user = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == customer.UserId,
                    cancellationToken);

            var allOrders = await _context.orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var farmerIds = allOrders
                .Select(o => o.FarmerId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(f => f.FarmerId, cancellationToken);

            var recentOrders = allOrders
                .Take(3)
                .Select(o =>
                {
                    farmers.TryGetValue(o.FarmerId, out var farmer);

                    return new CustomerRecentOrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderNo = o.OrderNo,
                        FarmerName = farmer?.BusinessName ?? "Farmer",
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus
                    };
                })
                .ToList();

            var favoriteProducts = await _context.favoriteproducts
                .AsNoTracking()
                .Where(fp => fp.CustomerId == customer.CustomerId)
                .OrderByDescending(fp => fp.CreatedAt)
                .Take(2)
                .ToListAsync(cancellationToken);

            var favoriteFarmerProductIds = favoriteProducts
                .Select(x => x.FarmerProductId)
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp =>
                    favoriteFarmerProductIds.Contains(fp.FarmerProductId))
                .ToListAsync(cancellationToken);

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var unitIds = farmerProducts
                .Select(fp => fp.UnitOfMeasureId)
                .Distinct()
                .ToList();

            var productFarmerIds = farmerProducts
                .Select(fp => fp.FarmerId)
                .Distinct()
                .ToList();

            var products = await _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId, cancellationToken);

            var units = await _context.unitofmeasures
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.UnitOfMeasureId))
                .ToDictionaryAsync(u => u.UnitOfMeasureId, cancellationToken);

            var productFarmers = await _context.farmers
                .AsNoTracking()
                .Where(f => productFarmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(f => f.FarmerId, cancellationToken);

            var productImages = await _context.productimages
                .AsNoTracking()
                .Where(pi => favoriteFarmerProductIds.Contains(pi.FarmerProductId))
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync(cancellationToken);

            var favoriteRows = farmerProducts
                .Select(fp =>
                {
                    products.TryGetValue(fp.ProductId, out var product);
                    units.TryGetValue(fp.UnitOfMeasureId, out var unit);
                    productFarmers.TryGetValue(fp.FarmerId, out var farmer);

                    var image = productImages
                        .FirstOrDefault(pi => pi.FarmerProductId == fp.FarmerProductId);

                    return new CustomerFavoriteProductViewModel
                    {
                        FarmerProductId = fp.FarmerProductId,
                        ProductName = product?.ProductName ?? "Product",
                        FarmerName = farmer?.BusinessName ?? "Farmer",
                        UnitName = unit?.UnitCode ?? unit?.UnitName ?? "",
                        Price = fp.Price,
                        ImageUrl = image?.ImageUrl ?? product?.DefaultImageUrl
                    };
                })
                .ToList();

            var favoriteMarkets = await _context.favoritemarkets
                .AsNoTracking()
                .Where(fm => fm.CustomerId == customer.CustomerId)
                .OrderByDescending(fm => fm.CreatedAt)
                .Take(3)
                .ToListAsync(cancellationToken);

            var favoriteMarketIds = favoriteMarkets
                .Select(x => x.MarketId)
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => favoriteMarketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var savedMarketRows = favoriteMarkets
                .Where(fm => markets.ContainsKey(fm.MarketId))
                .Select(fm => new CustomerSavedMarketViewModel
                {
                    MarketId = fm.MarketId,
                    MarketName = markets[fm.MarketId].MarketName,
                    Address = markets[fm.MarketId].Address
                })
                .ToList();

            var firstOfMonth =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1);

            var activeStatuses =
                new[] { "Placed", "Accepted", "Ready for Pickup" };

            return View(new CustomerDashboardViewModel
            {
                CustomerName = user?.FullName ?? "Customer",

                TotalOrders = allOrders.Count,
                OrdersThisMonth =
                    allOrders.Count(o => o.OrderDate >= firstOfMonth),
                ActiveOrders =
                    allOrders.Count(o => activeStatuses.Contains(o.OrderStatus)),

                FavoriteProducts =
                    await _context.favoriteproducts
                        .AsNoTracking()
                        .CountAsync(
                            fp => fp.CustomerId == customer.CustomerId,
                            cancellationToken),

                SavedMarkets =
                    await _context.favoritemarkets
                        .AsNoTracking()
                        .CountAsync(
                            fm => fm.CustomerId == customer.CustomerId,
                            cancellationToken),

                RecentOrders = recentOrders,
                FavoriteProductRows = favoriteRows,
                SavedMarketRows = savedMarketRows
            });
        }

        private async Task<Customer?> GetCurrentCustomerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId,
                    cancellationToken);
        }
    }
}
