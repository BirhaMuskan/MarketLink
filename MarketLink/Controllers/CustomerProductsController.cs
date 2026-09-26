using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            int? marketId,
            int? farmerId,
            bool availableOnly = false,
            CancellationToken cancellationToken = default)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => fp.IsApproved && fp.IsActive)
                .ToListAsync(cancellationToken);

            var activeFarmers = await _context.farmers
                .AsNoTracking()
                .Where(f => f.IsApproved && f.IsActive)
                .ToListAsync(cancellationToken);

            var activeFarmerIds = activeFarmers
                .Select(f => f.FarmerId)
                .ToHashSet();

            farmerProducts = farmerProducts
                .Where(fp => activeFarmerIds.Contains(fp.FarmerId))
                .ToList();

            if (farmerId.HasValue)
            {
                farmerProducts = farmerProducts
                    .Where(fp => fp.FarmerId == farmerId.Value)
                    .ToList();
            }

            if (marketId.HasValue)
            {
                var farmersAtMarket = await _context.farmermarkets
                    .AsNoTracking()
                    .Where(fm => fm.MarketId == marketId.Value &&
                                 fm.IsActive)
                    .Select(fm => fm.FarmerId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                farmerProducts = farmerProducts
                    .Where(fp => farmersAtMarket.Contains(fp.FarmerId))
                    .ToList();
            }

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var productsQuery = _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId) &&
                            p.IsActive);

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery
                    .Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                productsQuery = productsQuery.Where(p =>
                    p.ProductName.Contains(term) ||
                    p.Description.Contains(term));
            }

            var products = await productsQuery
                .ToListAsync(cancellationToken);

            var filteredProductIds = products
                .Select(p => p.ProductId)
                .ToHashSet();

            farmerProducts = farmerProducts
                .Where(fp => filteredProductIds.Contains(fp.ProductId))
                .ToList();

            var cards = await BuildProductCardsAsync(
                customer.CustomerId,
                farmerProducts,
                products,
                activeFarmers,
                marketId,
                cancellationToken);

            if (availableOnly)
            {
                cards = cards
                    .Where(x => x.IsAvailable)
                    .ToList();
            }

            cards = cards
                .OrderByDescending(x => x.IsAvailable)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.FarmerName)
                .ToList();

            var categories = await _context.categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync(cancellationToken);

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.MarketName)
                .ToListAsync(cancellationToken);

            return View(new CustomerProductsPageViewModel
            {
                Search = search,
                CategoryId = categoryId,
                MarketId = marketId,
                FarmerId = farmerId,
                AvailableOnly = availableOnly,

                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName,
                    Selected = c.CategoryId == categoryId
                }).ToList(),

                Markets = markets.Select(m => new SelectListItem
                {
                    Value = m.MarketId.ToString(),
                    Text = m.MarketName,
                    Selected = m.MarketId == marketId
                }).ToList(),

                Farmers = activeFarmers
                    .OrderBy(f => f.BusinessName)
                    .Select(f => new SelectListItem
                    {
                        Value = f.FarmerId.ToString(),
                        Text = f.BusinessName,
                        Selected = f.FarmerId == farmerId
                    })
                    .ToList(),

                Products = cards,
                TotalProducts = cards.Count,
                AvailableProducts = cards.Count(x => x.IsAvailable),

                FavoriteProducts = await _context.favoriteproducts
                    .AsNoTracking()
                    .CountAsync(
                        fp => fp.CustomerId == customer.CustomerId,
                        cancellationToken)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var farmerProduct = await _context.farmerproducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fp => fp.FarmerProductId == id &&
                          fp.IsApproved &&
                          fp.IsActive,
                    cancellationToken);

            if (farmerProduct == null)
                return NotFound();

            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FarmerId == farmerProduct.FarmerId &&
                         f.IsApproved &&
                         f.IsActive,
                    cancellationToken);

            if (farmer == null)
                return NotFound();

            var product = await _context.products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductId == farmerProduct.ProductId &&
                         p.IsActive,
                    cancellationToken);

            if (product == null)
                return NotFound();

            var category = await _context.categories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CategoryId == product.CategoryId,
                    cancellationToken);

            var unit = await _context.unitofmeasures
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UnitOfMeasureId == farmerProduct.UnitOfMeasureId,
                    cancellationToken);

            var images = await _context.productimages
                .AsNoTracking()
                .Where(pi => pi.FarmerProductId == farmerProduct.FarmerProductId)
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync(cancellationToken);

            var inventories = await _context.inventories
                .AsNoTracking()
                .Where(i => i.FarmerProductId == farmerProduct.FarmerProductId &&
                            i.InventoryDate >= DateTime.Today)
                .OrderBy(i => i.InventoryDate)
                .ToListAsync(cancellationToken);

            var farmerMarketIds = inventories
                .Select(i => i.FarmerMarketId)
                .Distinct()
                .ToList();

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => farmerMarketIds.Contains(fm.FarmerMarketId) &&
                             fm.IsActive)
                .ToDictionaryAsync(
                    fm => fm.FarmerMarketId,
                    cancellationToken);

            var marketIds = farmerMarkets.Values
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId) && m.IsActive)
                .ToDictionaryAsync(
                    m => m.MarketId,
                    cancellationToken);

            var inventoryRows = inventories
                .Where(i => farmerMarkets.ContainsKey(i.FarmerMarketId))
                .Select(i =>
                {
                    var fm = farmerMarkets[i.FarmerMarketId];
                    markets.TryGetValue(fm.MarketId, out var market);

                    var available = Math.Max(
                        0,
                        i.StockQuantity -
                        i.ReservedQuantity -
                        i.SoldQuantity);

                    return new CustomerProductInventoryViewModel
                    {
                        InventoryId = i.InventoryId,
                        FarmerMarketId = i.FarmerMarketId,
                        MarketId = fm.MarketId,
                        MarketName = market?.MarketName ?? "Market",
                        MarketAddress = market?.Address ?? "",
                        InventoryDate = i.InventoryDate,
                        UnitPrice = i.UnitPrice,
                        StockQuantity = i.StockQuantity,
                        ReservedQuantity = i.ReservedQuantity,
                        SoldQuantity = i.SoldQuantity,
                        AvailableQuantity = available,
                        IsAvailable =
                            i.IsAvailable &&
                            !i.IsSoldOut &&
                            available > 0,
                        IsSoldOut =
                            i.IsSoldOut ||
                            available <= 0
                    };
                })
                .ToList();

            var isFavorite = await _context.favoriteproducts
                .AsNoTracking()
                .AnyAsync(
                    fp => fp.CustomerId == customer.CustomerId &&
                          fp.FarmerProductId == farmerProduct.FarmerProductId,
                    cancellationToken);

            var imageUrls = images
                .Select(i => i.ImageUrl)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (imageUrls.Count == 0 &&
                !string.IsNullOrWhiteSpace(product.DefaultImageUrl))
            {
                imageUrls.Add(product.DefaultImageUrl);
            }

            return View(new CustomerProductDetailsViewModel
            {
                FarmerProductId = farmerProduct.FarmerProductId,
                FarmerId = farmer.FarmerId,
                ProductName = product.ProductName,
                CategoryName = category?.CategoryName ?? "Uncategorized",
                FarmerName = farmer.BusinessName,
                UnitName = unit?.UnitCode ?? unit?.UnitName ?? "",
                Description =
                    string.IsNullOrWhiteSpace(farmerProduct.FarmerDescription)
                        ? product.Description
                        : farmerProduct.FarmerDescription,
                ImageUrl = imageUrls.FirstOrDefault(),
                BasePrice = farmerProduct.Price,
                IsFavorite = isFavorite,
                Images = imageUrls,
                Inventory = inventoryRows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(
            int farmerProductId,
            string? returnUrl,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var productExists = await _context.farmerproducts
                .AsNoTracking()
                .AnyAsync(
                    fp => fp.FarmerProductId == farmerProductId &&
                          fp.IsApproved &&
                          fp.IsActive,
                    cancellationToken);

            if (!productExists)
                return NotFound();

            var favorite = await _context.favoriteproducts
                .FirstOrDefaultAsync(
                    fp => fp.CustomerId == customer.CustomerId &&
                          fp.FarmerProductId == farmerProductId,
                    cancellationToken);

            if (favorite == null)
            {
                _context.favoriteproducts.Add(
                    new FavoriteProduct
                    {
                        CustomerId = customer.CustomerId,
                        FarmerProductId = farmerProductId,
                        RestockAlert = true,
                        CreatedAt = DateTime.Now
                    });
            }
            else
            {
                _context.favoriteproducts.Remove(favorite);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(
                nameof(Details),
                new { id = farmerProductId });
        }

        private async Task<List<CustomerProductCardViewModel>> BuildProductCardsAsync(
            int customerId,
            List<FarmerProduct> farmerProducts,
            List<Product> products,
            List<Farmer> farmers,
            int? marketId,
            CancellationToken cancellationToken)
        {
            if (farmerProducts.Count == 0)
                return new();

            var farmerProductIds = farmerProducts
                .Select(fp => fp.FarmerProductId)
                .ToList();

            var productMap = products
                .ToDictionary(p => p.ProductId);

            var categoryIds = products
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

            var farmerMap = farmers
                .ToDictionary(f => f.FarmerId);

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

            List<FarmerMarket> farmerMarketRows;

            if (marketId.HasValue)
            {
                farmerMarketRows = await _context.farmermarkets
                    .AsNoTracking()
                    .Where(fm => fm.MarketId == marketId.Value &&
                                 fm.IsActive)
                    .ToListAsync(cancellationToken);

                var allowedIds = farmerMarketRows
                    .Select(fm => fm.FarmerMarketId)
                    .ToList();

                inventoryQuery = inventoryQuery
                    .Where(i => allowedIds.Contains(i.FarmerMarketId));
            }
            else
            {
                farmerMarketRows = await _context.farmermarkets
                    .AsNoTracking()
                    .Where(fm => fm.IsActive)
                    .ToListAsync(cancellationToken);
            }

            var farmerMarketMap = farmerMarketRows
                .ToDictionary(fm => fm.FarmerMarketId);

            var inventories = await inventoryQuery
                .ToListAsync(cancellationToken);

            var marketIds = farmerMarketRows
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var marketMap = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId) && m.IsActive)
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var favorites = await _context.favoriteproducts
                .AsNoTracking()
                .Where(fp => fp.CustomerId == customerId &&
                             farmerProductIds.Contains(fp.FarmerProductId))
                .Select(fp => fp.FarmerProductId)
                .ToListAsync(cancellationToken);

            var result = new List<CustomerProductCardViewModel>();

            foreach (var fp in farmerProducts)
            {
                if (!productMap.TryGetValue(fp.ProductId, out var product))
                    continue;

                units.TryGetValue(fp.UnitOfMeasureId, out var unit);
                categories.TryGetValue(product.CategoryId, out var category);
                farmerMap.TryGetValue(fp.FarmerId, out var farmer);

                var fpInventories = inventories
                    .Where(i => i.FarmerProductId == fp.FarmerProductId &&
                                farmerMarketMap.ContainsKey(i.FarmerMarketId))
                    .ToList();

                var availableQty = fpInventories
                    .Where(i => i.IsAvailable && !i.IsSoldOut)
                    .Sum(i => Math.Max(
                        0,
                        i.StockQuantity -
                        i.ReservedQuantity -
                        i.SoldQuantity));

                var marketNames = fpInventories
                    .Select(i =>
                    {
                        var fm = farmerMarketMap[i.FarmerMarketId];
                        return marketMap.TryGetValue(fm.MarketId, out var m)
                            ? m.MarketName
                            : null;
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .Take(3)
                    .ToList();

                var image = images.FirstOrDefault(
                    i => i.FarmerProductId == fp.FarmerProductId);

                result.Add(new CustomerProductCardViewModel
                {
                    FarmerProductId = fp.FarmerProductId,
                    FarmerId = fp.FarmerId,
                    ProductId = fp.ProductId,
                    CategoryId = product.CategoryId,
                    ProductName = product.ProductName,
                    CategoryName = category?.CategoryName ?? "Uncategorized",
                    FarmerName = farmer?.BusinessName ?? "Farmer",
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
                        marketNames.Count == 0
                            ? "No current dated stock"
                            : string.Join(", ", marketNames),
                    IsFavorite = favorites.Contains(fp.FarmerProductId)
                });
            }

            return result;
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
