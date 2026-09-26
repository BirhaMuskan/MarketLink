using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerFavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerFavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var favoriteFarmers = await _context.favoritefarmers
                .AsNoTracking()
                .Where(ff => ff.CustomerId == customer.CustomerId)
                .OrderByDescending(ff => ff.CreatedAt)
                .ToListAsync(cancellationToken);

            var farmerIds = favoriteFarmers
                .Select(ff => ff.FarmerId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f =>
                    farmerIds.Contains(f.FarmerId) &&
                    f.IsApproved &&
                    f.IsActive)
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var favoriteFarmerRows = favoriteFarmers
                .Where(ff => farmers.ContainsKey(ff.FarmerId))
                .Select(ff =>
                {
                    var farmer = farmers[ff.FarmerId];

                    return new CustomerFavoriteFarmerViewModel
                    {
                        FavoriteFarmerId = ff.FavoriteFarmerId,
                        FarmerId = farmer.FarmerId,
                        BusinessName = farmer.BusinessName,
                        Address = farmer.Address,
                        Description = farmer.Description,
                        ProfileImageUrl = farmer.ProfileImageUrl
                    };
                })
                .ToList();

            var favoriteProducts = await _context.favoriteproducts
                .AsNoTracking()
                .Where(fp => fp.CustomerId == customer.CustomerId)
                .OrderByDescending(fp => fp.CreatedAt)
                .ToListAsync(cancellationToken);

            var farmerProductIds = favoriteProducts
                .Select(fp => fp.FarmerProductId)
                .Distinct()
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp =>
                    farmerProductIds.Contains(fp.FarmerProductId) &&
                    fp.IsApproved &&
                    fp.IsActive)
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
                .ToDictionaryAsync(
                    p => p.ProductId,
                    cancellationToken);

            var units = await _context.unitofmeasures
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.UnitOfMeasureId))
                .ToDictionaryAsync(
                    u => u.UnitOfMeasureId,
                    cancellationToken);

            var productFarmers = await _context.farmers
                .AsNoTracking()
                .Where(f =>
                    productFarmerIds.Contains(f.FarmerId) &&
                    f.IsApproved &&
                    f.IsActive)
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var images = await _context.productimages
                .AsNoTracking()
                .Where(pi =>
                    farmerProductIds.Contains(pi.FarmerProductId))
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync(cancellationToken);

            var inventories = await _context.inventories
                .AsNoTracking()
                .Where(i =>
                    farmerProductIds.Contains(i.FarmerProductId) &&
                    i.InventoryDate >= DateTime.Today)
                .ToListAsync(cancellationToken);

            var favoriteProductRows = new List<CustomerFavoriteProductPageViewModel>();

            foreach (var favorite in favoriteProducts)
            {
                var fp = farmerProducts
                    .FirstOrDefault(x =>
                        x.FarmerProductId == favorite.FarmerProductId);

                if (fp == null)
                    continue;

                products.TryGetValue(fp.ProductId, out var product);
                units.TryGetValue(fp.UnitOfMeasureId, out var unit);
                productFarmers.TryGetValue(fp.FarmerId, out var farmer);

                if (product == null || farmer == null)
                    continue;

                var image = images
                    .FirstOrDefault(i =>
                        i.FarmerProductId == fp.FarmerProductId);

                var availableQuantity = inventories
                    .Where(i =>
                        i.FarmerProductId == fp.FarmerProductId &&
                        i.IsAvailable &&
                        !i.IsSoldOut)
                    .Sum(i => Math.Max(
                        0,
                        i.StockQuantity -
                        i.ReservedQuantity -
                        i.SoldQuantity));

                favoriteProductRows.Add(
                    new CustomerFavoriteProductPageViewModel
                    {
                        FavoriteProductId = favorite.FavoriteProductId,
                        FarmerProductId = fp.FarmerProductId,
                        FarmerId = fp.FarmerId,
                        ProductName = product.ProductName,
                        FarmerName = farmer.BusinessName,
                        UnitName =
                            unit?.UnitCode ??
                            unit?.UnitName ??
                            "",
                        ImageUrl =
                            image?.ImageUrl ??
                            product.DefaultImageUrl,
                        Price = fp.Price,
                        AvailableQuantity = availableQuantity,
                        IsAvailable =
                            fp.IsAvailable &&
                            availableQuantity > 0,
                        RestockAlert = favorite.RestockAlert
                    });
            }

            var favoriteMarkets = await _context.favoritemarkets
                .AsNoTracking()
                .Where(fm => fm.CustomerId == customer.CustomerId)
                .OrderByDescending(fm => fm.CreatedAt)
                .ToListAsync(cancellationToken);

            var favoriteMarketIds = favoriteMarkets
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m =>
                    favoriteMarketIds.Contains(m.MarketId) &&
                    m.IsActive)
                .ToDictionaryAsync(
                    m => m.MarketId,
                    cancellationToken);

            var marketDays = await _context.marketdays
                .AsNoTracking()
                .Where(md =>
                    favoriteMarketIds.Contains(md.MarketId) &&
                    md.IsActive)
                .ToListAsync(cancellationToken);

            var favoriteMarketRows = favoriteMarkets
                .Where(fm => markets.ContainsKey(fm.MarketId))
                .Select(fm =>
                {
                    var market = markets[fm.MarketId];

                    var days = marketDays
                        .Where(md => md.MarketId == market.MarketId)
                        .OrderBy(md => DayOrder(md.DayName))
                        .Select(md => md.DayName)
                        .ToList();

                    return new CustomerFavoriteMarketPageViewModel
                    {
                        FavoriteMarketId = fm.FavoriteMarketId,
                        MarketId = market.MarketId,
                        MarketName = market.MarketName,
                        Address = market.Address,
                        OperatingDaysText =
                            days.Count == 0
                                ? "Schedule not available"
                                : string.Join(", ", days)
                    };
                })
                .ToList();

            return View(new CustomerFavoritesPageViewModel
            {
                FavoriteFarmersCount = favoriteFarmerRows.Count,
                FavoriteProductsCount = favoriteProductRows.Count,
                FavoriteMarketsCount = favoriteMarketRows.Count,

                Farmers = favoriteFarmerRows,
                Products = favoriteProductRows,
                Markets = favoriteMarketRows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFarmer(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var favorite = await _context.favoritefarmers
                .FirstOrDefaultAsync(
                    ff =>
                        ff.FavoriteFarmerId == id &&
                        ff.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (favorite == null)
                return NotFound();

            _context.favoritefarmers.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Farmer removed from favorites.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var favorite = await _context.favoriteproducts
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FavoriteProductId == id &&
                        fp.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (favorite == null)
                return NotFound();

            _context.favoriteproducts.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Product removed from favorites.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMarket(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var favorite = await _context.favoritemarkets
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FavoriteMarketId == id &&
                        fm.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (favorite == null)
                return NotFound();

            _context.favoritemarkets.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Market removed from favorites.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRestockAlert(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var favorite = await _context.favoriteproducts
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FavoriteProductId == id &&
                        fp.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (favorite == null)
                return NotFound();

            favorite.RestockAlert = !favorite.RestockAlert;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                favorite.RestockAlert
                    ? "Restock alert enabled."
                    : "Restock alert disabled.";

            return RedirectToAction(nameof(Index));
        }

        private static int DayOrder(string dayName)
        {
            return dayName switch
            {
                "Monday" => 1,
                "Tuesday" => 2,
                "Wednesday" => 3,
                "Thursday" => 4,
                "Friday" => 5,
                "Saturday" => 6,
                "Sunday" => 7,
                _ => 99
            };
        }

        private async Task<Customer?> GetCurrentCustomerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
                return null;

            return await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId,
                    cancellationToken);
        }
    }
}
