using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerWeeklyStockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerWeeklyStockController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
                return Forbid();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp =>
                    fp.FarmerId == farmer.FarmerId &&
                    fp.IsActive)
                .ToListAsync(cancellationToken);

            var farmerMarketDays = await _context.farmermarketdays
                .AsNoTracking()
                .Where(fmd =>
                    fmd.FarmerMarket != null &&
                    fmd.FarmerMarket.FarmerId == farmer.FarmerId &&
                    fmd.FarmerMarket.IsActive &&
                    fmd.IsActive)
                .ToListAsync(cancellationToken);

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var unitIds = farmerProducts
                .Select(fp => fp.UnitOfMeasureId)
                .Distinct()
                .ToList();

            var marketDayIds = farmerMarketDays
                .Select(fmd => fmd.MarketDayId)
                .Distinct()
                .ToList();

            var farmerMarketIds = farmerMarketDays
                .Select(fmd => fmd.FarmerMarketId)
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

            var marketDays = await _context.marketdays
                .AsNoTracking()
                .Where(md => marketDayIds.Contains(md.MarketDayId))
                .ToDictionaryAsync(md => md.MarketDayId, cancellationToken);

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => farmerMarketIds.Contains(fm.FarmerMarketId))
                .ToDictionaryAsync(fm => fm.FarmerMarketId, cancellationToken);

            var marketIds = farmerMarkets.Values
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var farmerProductIds = farmerProducts
                .Select(fp => fp.FarmerProductId)
                .ToList();

            var templates = await _context.recurringstocktemplates
                .AsNoTracking()
                .Where(t => farmerProductIds.Contains(t.FarmerProductId))
                .ToListAsync(cancellationToken);

            var inventories = await _context.inventories
                .AsNoTracking()
                .Where(i =>
                    farmerProductIds.Contains(i.FarmerProductId) &&
                    farmerMarketIds.Contains(i.FarmerMarketId) &&
                    i.InventoryDate >= DateTime.Today)
                .ToListAsync(cancellationToken);

            var pickupSlots = await _context.pickupslots
                .AsNoTracking()
                .Where(ps =>
                    farmerMarketIds.Contains(ps.FarmerMarketId) &&
                    ps.PickupDate >= DateTime.Today)
                .ToListAsync(cancellationToken);

            var rows = new List<FarmerWeeklyStockRowViewModel>();

            foreach (var fp in farmerProducts)
            {
                products.TryGetValue(fp.ProductId, out var product);
                units.TryGetValue(fp.UnitOfMeasureId, out var unit);

                foreach (var fmd in farmerMarketDays)
                {
                    if (!farmerMarkets.TryGetValue(
                        fmd.FarmerMarketId,
                        out var fm))
                        continue;

                    if (!marketDays.TryGetValue(
                        fmd.MarketDayId,
                        out var md))
                        continue;

                    if (!markets.TryGetValue(
                        fm.MarketId,
                        out var market))
                        continue;

                    var template = templates
                        .FirstOrDefault(t =>
                            t.FarmerProductId == fp.FarmerProductId &&
                            t.FarmerMarketDayId == fmd.FarmerMarketDayId);

                    var nextDate =
                        GetNextDateForDayName(
                            DateTime.Today,
                            md.DayName);

                    var inventoryExists = inventories.Any(i =>
                        i.FarmerProductId == fp.FarmerProductId &&
                        i.FarmerMarketId == fmd.FarmerMarketId &&
                        i.InventoryDate.Date == nextDate.Date);

                    var pickupSlotExists = pickupSlots.Any(ps =>
                        ps.FarmerMarketId == fmd.FarmerMarketId &&
                        ps.PickupDate.Date == nextDate.Date &&
                        ps.IsAvailable &&
                        ps.BookedOrders < ps.MaximumOrders);

                    rows.Add(new FarmerWeeklyStockRowViewModel
                    {
                        RecurringStockTemplateId =
                            template?.RecurringStockTemplateId,

                        FarmerProductId = fp.FarmerProductId,
                        FarmerMarketDayId = fmd.FarmerMarketDayId,
                        FarmerMarketId = fmd.FarmerMarketId,

                        ProductName = product?.ProductName ?? "Product",
                        UnitName = unit?.UnitCode ?? unit?.UnitName ?? "",
                        MarketName = market.MarketName,
                        DayName = md.DayName,

                        PickupStartTime = fmd.PickupStartTime,
                        PickupEndTime = fmd.PickupEndTime,

                        DefaultQuantity =
                            template?.DefaultQuantity ?? 0,

                        IsConfigured = template != null,
                        IsActive = template?.IsActive ?? true,

                        NextInventoryDate = nextDate,
                        NextInventoryExists = inventoryExists,
                        NextPickupSlotExists = pickupSlotExists
                    });
                }
            }

            rows = rows
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.MarketName)
                .ThenBy(x => DayOrder(x.DayName))
                .ToList();

            return View(new FarmerWeeklyStockPageViewModel
            {
                Rows = rows,
                TotalTemplates =
                    rows.Count(x => x.IsConfigured),
                ActiveTemplates =
                    rows.Count(x => x.IsConfigured && x.IsActive),
                TotalDefaultQuantity =
                    rows
                        .Where(x => x.IsConfigured && x.IsActive)
                        .Sum(x => x.DefaultQuantity)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            SaveWeeklyStockViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
                return Forbid();

            var farmerProduct = await _context.farmerproducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == model.FarmerProductId &&
                        fp.FarmerId == farmer.FarmerId &&
                        fp.IsActive,
                    cancellationToken);

            if (farmerProduct == null)
                return NotFound();

            var farmerMarketDay = await _context.farmermarketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fmd =>
                        fmd.FarmerMarketDayId == model.FarmerMarketDayId &&
                        fmd.FarmerMarket != null &&
                        fmd.FarmerMarket.FarmerId == farmer.FarmerId &&
                        fmd.FarmerMarket.IsActive,
                    cancellationToken);

            if (farmerMarketDay == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Please enter a valid weekly quantity.";

                return RedirectToAction(nameof(Index));
            }

            var template = await _context.recurringstocktemplates
                .FirstOrDefaultAsync(
                    t =>
                        t.FarmerProductId == model.FarmerProductId &&
                        t.FarmerMarketDayId == model.FarmerMarketDayId,
                    cancellationToken);

            if (template == null)
            {
                template = new RecurringStockTemplate
                {
                    FarmerProductId = model.FarmerProductId,
                    FarmerMarketDayId = model.FarmerMarketDayId
                };

                _context.recurringstocktemplates.Add(template);
            }

            template.DefaultQuantity = model.DefaultQuantity;
            template.IsActive = model.IsActive;
            template.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Weekly stock template saved.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateNextInventory(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
                return Forbid();

            var template = await _context.recurringstocktemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.RecurringStockTemplateId == id,
                    cancellationToken);

            if (template == null || !template.IsActive)
            {
                TempData["ErrorMessage"] =
                    "Weekly stock template was not found or is paused.";

                return RedirectToAction(nameof(Index));
            }

            var farmerProduct = await _context.farmerproducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == template.FarmerProductId &&
                        fp.FarmerId == farmer.FarmerId &&
                        fp.IsActive,
                    cancellationToken);

            if (farmerProduct == null)
                return NotFound();

            var farmerMarketDay = await _context.farmermarketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fmd =>
                        fmd.FarmerMarketDayId == template.FarmerMarketDayId &&
                        fmd.FarmerMarket != null &&
                        fmd.FarmerMarket.FarmerId == farmer.FarmerId &&
                        fmd.FarmerMarket.IsActive &&
                        fmd.IsActive,
                    cancellationToken);

            if (farmerMarketDay == null)
                return NotFound();

            var marketDay = await _context.marketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    md =>
                        md.MarketDayId == farmerMarketDay.MarketDayId &&
                        md.IsActive,
                    cancellationToken);

            if (marketDay == null)
                return NotFound();

            var nextDate =
                GetNextDateForDayName(
                    DateTime.Today,
                    marketDay.DayName);

            var duplicate = await _context.inventories
                .AsNoTracking()
                .AnyAsync(
                    i =>
                        i.FarmerProductId == farmerProduct.FarmerProductId &&
                        i.FarmerMarketId == farmerMarketDay.FarmerMarketId &&
                        i.InventoryDate.Date == nextDate.Date,
                    cancellationToken);

            if (duplicate)
            {
                TempData["ErrorMessage"] =
                    $"Inventory already exists for {nextDate:dd MMM yyyy}.";

                return RedirectToAction(nameof(Index));
            }

            var inventory = new Inventory
            {
                FarmerProductId = farmerProduct.FarmerProductId,
                FarmerMarketId = farmerMarketDay.FarmerMarketId,
                InventoryDate = nextDate.Date,
                UnitPrice = farmerProduct.Price,
                StockQuantity = template.DefaultQuantity,
                ReservedQuantity = 0,
                SoldQuantity = 0,
                IsSoldOut = template.DefaultQuantity <= 0,
                IsAvailable =
                    farmerProduct.IsAvailable &&
                    template.DefaultQuantity > 0,
                UpdatedAt = DateTime.Now
            };

            _context.inventories.Add(inventory);

            await _context.SaveChangesAsync(cancellationToken);

            var hasPickupSlot = await _context.pickupslots
                .AsNoTracking()
                .AnyAsync(
                    ps =>
                        ps.FarmerMarketId == farmerMarketDay.FarmerMarketId &&
                        ps.PickupDate.Date == nextDate.Date &&
                        ps.IsAvailable &&
                        ps.BookedOrders < ps.MaximumOrders,
                    cancellationToken);

            TempData["SuccessMessage"] = hasPickupSlot
                ? $"Inventory generated for {nextDate:dd MMM yyyy}. Matching pickup slot found."
                : $"Inventory generated for {nextDate:dd MMM yyyy}. Create a pickup slot for the same date before customers can checkout.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
                return Forbid();

            var template = await _context.recurringstocktemplates
                .FirstOrDefaultAsync(
                    t =>
                        t.RecurringStockTemplateId == id &&
                        t.FarmerProduct != null &&
                        t.FarmerProduct.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (template == null)
                return NotFound();

            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                template.IsActive
                    ? "Weekly template activated."
                    : "Weekly template paused.";

            return RedirectToAction(nameof(Index));
        }

        private static DateTime GetNextDateForDayName(
            DateTime fromDate,
            string dayName)
        {
            if (!Enum.TryParse<DayOfWeek>(
                dayName,
                true,
                out var targetDay))
            {
                return fromDate.Date;
            }

            var daysAhead =
                ((int)targetDay -
                 (int)fromDate.DayOfWeek +
                 7) % 7;

            return fromDate.Date.AddDays(daysAhead);
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

        private async Task<Farmer?> GetCurrentFarmerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
                return null;

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
