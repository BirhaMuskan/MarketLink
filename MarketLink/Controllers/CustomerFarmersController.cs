using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerFarmersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerFarmersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            int? marketId,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FarmerId == id &&
                         f.IsApproved &&
                         f.IsActive,
                    cancellationToken);

            if (farmer == null)
                return NotFound();

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => fm.FarmerId == farmer.FarmerId &&
                             fm.IsActive)
                .ToListAsync(cancellationToken);

            if (marketId.HasValue)
            {
                farmerMarkets = farmerMarkets
                    .Where(fm => fm.MarketId == marketId.Value)
                    .ToList();
            }

            var marketIds = farmerMarkets
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId) && m.IsActive)
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var farmerMarketIds = farmerMarkets
                .Select(fm => fm.FarmerMarketId)
                .ToList();

            var farmerMarketDays = await _context.farmermarketdays
                .AsNoTracking()
                .Where(fmd => farmerMarketIds.Contains(fmd.FarmerMarketId) &&
                              fmd.IsActive)
                .ToListAsync(cancellationToken);

            var marketDayIds = farmerMarketDays
                .Select(fmd => fmd.MarketDayId)
                .Distinct()
                .ToList();

            var marketDays = await _context.marketdays
                .AsNoTracking()
                .Where(md => marketDayIds.Contains(md.MarketDayId) &&
                             md.IsActive)
                .ToDictionaryAsync(md => md.MarketDayId, cancellationToken);

            var marketRows = farmerMarkets
                .Where(fm => markets.ContainsKey(fm.MarketId))
                .Select(fm =>
                {
                    var days = farmerMarketDays
                        .Where(fmd => fmd.FarmerMarketId == fm.FarmerMarketId)
                        .Select(fmd =>
                            marketDays.TryGetValue(fmd.MarketDayId, out var day)
                                ? day.DayName
                                : null)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();

                    return new CustomerFarmerMarketViewModel
                    {
                        FarmerMarketId = fm.FarmerMarketId,
                        MarketId = fm.MarketId,
                        MarketName = markets[fm.MarketId].MarketName,
                        Address = markets[fm.MarketId].Address,
                        StallNumber = fm.StallNumber,
                        PickupInstructions = fm.PickupInstructions,
                        OperatingDaysText =
                            days.Count == 0
                                ? "Schedule not configured"
                                : string.Join(", ", days!)
                    };
                })
                .OrderBy(x => x.MarketName)
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => fp.FarmerId == farmer.FarmerId &&
                             fp.IsApproved &&
                             fp.IsActive)
                .ToListAsync(cancellationToken);

            var productCards = await BuildProductCardsAsync(
                customer.CustomerId,
                farmerProducts,
                marketId,
                cancellationToken);

            var isFavorite = await _context.favoritefarmers
                .AsNoTracking()
                .AnyAsync(
                    ff => ff.CustomerId == customer.CustomerId &&
                          ff.FarmerId == farmer.FarmerId,
                    cancellationToken);

            return View(new CustomerFarmerDetailsViewModel
            {
                FarmerId = farmer.FarmerId,
                BusinessName = farmer.BusinessName,
                Description = farmer.Description,
                Address = farmer.Address,
                ProfileImageUrl = farmer.ProfileImageUrl,
                Latitude = farmer.Latitude,
                Longitude = farmer.Longitude,
                IsFavorite = isFavorite,
                Markets = marketRows,
                Products = productCards
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(
            int farmerId,
            string? returnUrl,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var farmerExists = await _context.farmers
                .AsNoTracking()
                .AnyAsync(
                    f => f.FarmerId == farmerId &&
                         f.IsApproved &&
                         f.IsActive,
                    cancellationToken);

            if (!farmerExists)
                return NotFound();

            var favorite = await _context.favoritefarmers
                .FirstOrDefaultAsync(
                    ff => ff.CustomerId == customer.CustomerId &&
                          ff.FarmerId == farmerId,
                    cancellationToken);

            if (favorite == null)
            {
                _context.favoritefarmers.Add(
                    new FavoriteFarmer
                    {
                        CustomerId = customer.CustomerId,
                        FarmerId = farmerId,
                        CreatedAt = DateTime.Now
                    });
            }
            else
            {
                _context.favoritefarmers.Remove(favorite);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = farmerId });
        }

        private async Task<List<CustomerProductCardViewModel>> BuildProductCardsAsync(
            int customerId,
            List<FarmerProduct> farmerProducts,
            int? marketId,
            CancellationToken cancellationToken)
        {
            if (farmerProducts.Count == 0)
                return new();

            var farmerProductIds = farmerProducts
                .Select(fp => fp.FarmerProductId)
                .ToList();

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId) && p.IsActive)
                .ToDictionaryAsync(p => p.ProductId, cancellationToken);

            var categoryIds = products.Values
                .Select(p => p.CategoryId)
                .Distinct()
                .ToList();

            var categories = await _context.categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.CategoryId))
                .ToDictionaryAsync(c => c.CategoryId, cancellationToken);

            var unitIds = farmerProducts
                .Select(fp => fp.UnitOfMeasureId)
                .Distinct()
                .ToList();

            var units = await _context.unitofmeasures
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.UnitOfMeasureId))
                .ToDictionaryAsync(u => u.UnitOfMeasureId, cancellationToken);

            var images = await _context.productimages
                .AsNoTracking()
                .Where(pi => farmerProductIds.Contains(pi.FarmerProductId))
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync(cancellationToken);

            IQueryable<Inventory> inventoryQuery =
                _context.inventories
                    .AsNoTracking()
                    .Where(i => farmerProductIds.Contains(i.FarmerProductId) &&
                                i.InventoryDate >= DateTime.Today);

            if (marketId.HasValue)
            {
                var allowedFarmerMarketIds = await _context.farmermarkets
                    .AsNoTracking()
                    .Where(fm => fm.MarketId == marketId.Value &&
                                 fm.IsActive)
                    .Select(fm => fm.FarmerMarketId)
                    .ToListAsync(cancellationToken);

                inventoryQuery = inventoryQuery
                    .Where(i => allowedFarmerMarketIds.Contains(i.FarmerMarketId));
            }

            var inventories = await inventoryQuery
                .ToListAsync(cancellationToken);

            var favorites = await _context.favoriteproducts
                .AsNoTracking()
                .Where(fp => fp.CustomerId == customerId &&
                             farmerProductIds.Contains(fp.FarmerProductId))
                .Select(fp => fp.FarmerProductId)
                .ToListAsync(cancellationToken);

            return farmerProducts.Select(fp =>
            {
                if (!products.TryGetValue(fp.ProductId, out var product))
                    return null;

                units.TryGetValue(fp.UnitOfMeasureId, out var unit);
                categories.TryGetValue(product.CategoryId, out var category);

                var fpInventories = inventories
                    .Where(i => i.FarmerProductId == fp.FarmerProductId)
                    .ToList();

                var availableQty = fpInventories
                    .Where(i => i.IsAvailable && !i.IsSoldOut)
                    .Sum(i => Math.Max(
                        0,
                        i.StockQuantity -
                        i.ReservedQuantity -
                        i.SoldQuantity));

                var image = images.FirstOrDefault(
                    i => i.FarmerProductId == fp.FarmerProductId);

                return new CustomerProductCardViewModel
                {
                    FarmerProductId = fp.FarmerProductId,
                    FarmerId = fp.FarmerId,
                    ProductId = fp.ProductId,
                    CategoryId = product.CategoryId,
                    ProductName = product.ProductName,
                    CategoryName = category?.CategoryName ?? "Uncategorized",
                    FarmerName = farmerProducts.Count > 0 ? "" : "",
                    UnitName = unit?.UnitCode ?? unit?.UnitName ?? "",
                    Description =
                        string.IsNullOrWhiteSpace(fp.FarmerDescription)
                            ? product.Description
                            : fp.FarmerDescription,
                    ImageUrl = image?.ImageUrl ?? product.DefaultImageUrl,
                    Price = fp.Price,
                    AvailableQuantity = availableQty,
                    IsAvailable = fp.IsAvailable && availableQty > 0,
                    MarketSummary =
                        marketId.HasValue
                            ? "Selected market"
                            : "See product details for market stock",
                    IsFavorite = favorites.Contains(fp.FarmerProductId)
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
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
