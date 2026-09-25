using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerInventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerInventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INVENTORY LIST
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var inventoryRows = await _context.inventories
                .AsNoTracking()
                .Where(i =>
                    i.FarmerProduct != null &&
                    i.FarmerProduct.FarmerId == farmer.FarmerId)
                .OrderByDescending(i => i.UpdatedAt)
                .ToListAsync(cancellationToken);

            var farmerProductIds = inventoryRows
                .Select(i => i.FarmerProductId)
                .Distinct()
                .ToList();

            var farmerMarketIds = inventoryRows
                .Select(i => i.FarmerMarketId)
                .Distinct()
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => farmerProductIds.Contains(fp.FarmerProductId))
                .ToListAsync(cancellationToken);

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var unitIds = farmerProducts
                .Select(fp => fp.UnitOfMeasureId)
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

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => farmerMarketIds.Contains(fm.FarmerMarketId))
                .ToListAsync(cancellationToken);

            var marketIds = farmerMarkets
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var farmerProductsById = farmerProducts
                .ToDictionary(fp => fp.FarmerProductId);

            var farmerMarketsById = farmerMarkets
                .ToDictionary(fm => fm.FarmerMarketId);

            var rows = inventoryRows.Select(i =>
            {
                farmerProductsById.TryGetValue(
                    i.FarmerProductId,
                    out var fp);

                farmerMarketsById.TryGetValue(
                    i.FarmerMarketId,
                    out var fm);

                Product? product = null;
                UnitOfMeasure? unit = null;
                Market? market = null;

                if (fp != null)
                {
                    products.TryGetValue(fp.ProductId, out product);
                    units.TryGetValue(fp.UnitOfMeasureId, out unit);
                }

                if (fm != null)
                {
                    markets.TryGetValue(fm.MarketId, out market);
                }

                var available =
                    i.StockQuantity -
                    i.ReservedQuantity -
                    i.SoldQuantity;

                return new FarmerInventoryRowViewModel
                {
                    InventoryId = i.InventoryId,
                    FarmerProductId = i.FarmerProductId,
                    FarmerMarketId = i.FarmerMarketId,

                    ProductName =
                        product?.ProductName ?? "Product",

                    UnitName =
                        unit?.UnitCode ?? "",

                    MarketName =
                        market?.MarketName ?? "Market",

                    InventoryDate = i.InventoryDate,
                    UnitPrice = i.UnitPrice,
                    StockQuantity = i.StockQuantity,
                    ReservedQuantity = i.ReservedQuantity,
                    SoldQuantity = i.SoldQuantity,
                    AvailableQuantity = available,

                    IsSoldOut =
                        i.IsSoldOut || available <= 0,

                    IsAvailable =
                        i.IsAvailable && available > 0,

                    UpdatedAt = i.UpdatedAt
                };
            }).ToList();

            return View(new FarmerInventoryPageViewModel
            {
                Rows = rows,
                TotalRows = rows.Count,
                AvailableRows =
                    rows.Count(x => x.IsAvailable && !x.IsSoldOut),
                SoldOutRows =
                    rows.Count(x => x.IsSoldOut),

                TotalStock =
                    rows.Sum(x => x.StockQuantity),

                TotalAvailable =
                    rows.Sum(x => Math.Max(0, x.AvailableQuantity)),

                TotalReserved =
                    rows.Sum(x => x.ReservedQuantity),

                TotalSold =
                    rows.Sum(x => x.SoldQuantity)
            });
        }

        // =========================================================
        // EDIT INVENTORY
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var inventory = await _context.inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.InventoryId == id,
                    cancellationToken);

            if (inventory == null)
            {
                return NotFound();
            }

            var farmerProduct = await _context.farmerproducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == inventory.FarmerProductId &&
                        fp.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (farmerProduct == null)
            {
                return Forbid();
            }

            var product = await _context.products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductId == farmerProduct.ProductId,
                    cancellationToken);

            var unit = await _context.unitofmeasures
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u =>
                        u.UnitOfMeasureId ==
                        farmerProduct.UnitOfMeasureId,
                    cancellationToken);

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId ==
                        inventory.FarmerMarketId &&
                        fm.FarmerId == farmer.FarmerId,
                    cancellationToken);

            var market = farmerMarket == null
                ? null
                : await _context.markets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.MarketId == farmerMarket.MarketId,
                        cancellationToken);

            return View(new EditFarmerInventoryViewModel
            {
                InventoryId = inventory.InventoryId,
                ProductName = product?.ProductName ?? "Product",
                MarketName = market?.MarketName ?? "Market",
                UnitName = unit?.UnitCode ?? "",
                StockQuantity = inventory.StockQuantity,
                UnitPrice = inventory.UnitPrice,
                IsAvailable = inventory.IsAvailable
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditFarmerInventoryViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var inventory = await _context.inventories
                .FirstOrDefaultAsync(
                    i => i.InventoryId == model.InventoryId,
                    cancellationToken);

            if (inventory == null)
            {
                return NotFound();
            }

            var farmerProduct = await _context.farmerproducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId ==
                        inventory.FarmerProductId &&
                        fp.FarmerId ==
                        farmer.FarmerId,
                    cancellationToken);

            if (farmerProduct == null)
            {
                return Forbid();
            }

            if (model.StockQuantity <
                inventory.ReservedQuantity +
                inventory.SoldQuantity)
            {
                ModelState.AddModelError(
                    nameof(model.StockQuantity),
                    "Stock quantity cannot be less than already reserved plus sold quantity.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateEditDisplayAsync(
                    model,
                    inventory,
                    farmerProduct,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }

            var previousQuantity = inventory.StockQuantity;

            inventory.StockQuantity = model.StockQuantity;
            inventory.UnitPrice = model.UnitPrice;

            var available =
                inventory.StockQuantity -
                inventory.ReservedQuantity -
                inventory.SoldQuantity;

            inventory.IsSoldOut = available <= 0;
            inventory.IsAvailable =
                model.IsAvailable && available > 0;

            inventory.UpdatedAt = DateTime.Now;

            _context.inventoryhistories.Add(
                new InventoryHistory
                {
                    InventoryId = inventory.InventoryId,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = inventory.StockQuantity,
                    QuantityChanged =
                        inventory.StockQuantity -
                        previousQuantity,
                    ChangeType = "FarmerUpdate",
                    Remarks =
                        string.IsNullOrWhiteSpace(model.Remarks)
                            ? "Inventory updated by farmer."
                            : model.Remarks.Trim(),
                    ChangedAt = DateTime.Now
                });

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Inventory updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSoldOut(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var inventory = await _context.inventories
                .FirstOrDefaultAsync(
                    i => i.InventoryId == id,
                    cancellationToken);

            if (inventory == null)
            {
                return NotFound();
            }

            var owned = await _context.farmerproducts
                .AsNoTracking()
                .AnyAsync(
                    fp =>
                        fp.FarmerProductId ==
                        inventory.FarmerProductId &&
                        fp.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (!owned)
            {
                return Forbid();
            }

            inventory.IsSoldOut = true;
            inventory.IsAvailable = false;
            inventory.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Item marked as sold out.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var inventory = await _context.inventories
                .FirstOrDefaultAsync(
                    i => i.InventoryId == id,
                    cancellationToken);

            if (inventory == null)
            {
                return NotFound();
            }

            var owned = await _context.farmerproducts
                .AsNoTracking()
                .AnyAsync(
                    fp =>
                        fp.FarmerProductId ==
                        inventory.FarmerProductId &&
                        fp.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (!owned)
            {
                return Forbid();
            }

            var available =
                inventory.StockQuantity -
                inventory.ReservedQuantity -
                inventory.SoldQuantity;

            if (!inventory.IsAvailable && available <= 0)
            {
                TempData["ErrorMessage"] =
                    "Increase stock quantity before making this item available.";

                return RedirectToAction(nameof(Index));
            }

            inventory.IsAvailable = !inventory.IsAvailable;
            inventory.IsSoldOut =
                inventory.IsAvailable
                    ? false
                    : inventory.IsSoldOut;

            inventory.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                inventory.IsAvailable
                    ? "Item is now available."
                    : "Item is temporarily unavailable.";

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateEditDisplayAsync(
            EditFarmerInventoryViewModel model,
            Inventory inventory,
            FarmerProduct farmerProduct,
            int farmerId,
            CancellationToken cancellationToken)
        {
            var product = await _context.products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductId == farmerProduct.ProductId,
                    cancellationToken);

            var unit = await _context.unitofmeasures
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u =>
                        u.UnitOfMeasureId ==
                        farmerProduct.UnitOfMeasureId,
                    cancellationToken);

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId ==
                        inventory.FarmerMarketId &&
                        fm.FarmerId == farmerId,
                    cancellationToken);

            var market = farmerMarket == null
                ? null
                : await _context.markets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.MarketId == farmerMarket.MarketId,
                        cancellationToken);

            model.ProductName =
                product?.ProductName ?? "Product";

            model.UnitName =
                unit?.UnitCode ?? "";

            model.MarketName =
                market?.MarketName ?? "Market";
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
