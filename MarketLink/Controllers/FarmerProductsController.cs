using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private const long MaxImageBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public FarmerProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =========================================================
        // MY PRODUCTS
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

            var listings = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => fp.FarmerId == farmer.FarmerId)
                .OrderByDescending(fp => fp.CreatedAt)
                .Select(fp => new
                {
                    fp.FarmerProductId,
                    fp.Price,
                    fp.FarmerDescription,
                    fp.IsAvailable,
                    fp.IsActive,
                    fp.CreatedAt,

                    ProductName =
                        fp.Product != null
                            ? fp.Product.ProductName
                            : "Product",

                    CategoryName =
                        fp.Product != null &&
                        fp.Product.Category != null
                            ? fp.Product.Category.CategoryName
                            : "Uncategorized",

                    UnitName =
                        fp.UnitOfMeasure != null
                            ? fp.UnitOfMeasure.UnitCode
                            : ""
                })
                .ToListAsync(cancellationToken);

            var listingIds = listings
                .Select(x => x.FarmerProductId)
                .ToList();

            var inventoryRows = await _context.inventories
                .AsNoTracking()
                .Where(i => listingIds.Contains(i.FarmerProductId))
                .OrderByDescending(i => i.UpdatedAt)
                .ToListAsync(cancellationToken);

            var imageRows = await _context.productimages
                .AsNoTracking()
                .Where(pi => listingIds.Contains(pi.FarmerProductId))
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync(cancellationToken);

            var rows = listings.Select(x =>
            {
                // One current stock record per market for this listing.
                var latestByMarket = inventoryRows
                    .Where(i => i.FarmerProductId == x.FarmerProductId)
                    .GroupBy(i => i.FarmerMarketId)
                    .Select(g => g
                        .OrderByDescending(i => i.UpdatedAt)
                        .First())
                    .ToList();

                var availableQuantity = latestByMarket.Sum(i =>
                    i.StockQuantity -
                    i.ReservedQuantity -
                    i.SoldQuantity);

                var imageUrl = imageRows
                    .FirstOrDefault(pi =>
                        pi.FarmerProductId == x.FarmerProductId)
                    ?.ImageUrl;

                return new FarmerProductRowViewModel
                {
                    FarmerProductId = x.FarmerProductId,
                    ProductName = x.ProductName,
                    CategoryName = x.CategoryName,
                    UnitName = x.UnitName,
                    Price = x.Price,
                    AvailableQuantity = availableQuantity,
                    FarmerDescription = x.FarmerDescription,
                    ImageUrl = imageUrl,
                    IsAvailable = x.IsAvailable,
                    IsActive = x.IsActive,
                    IsSoldOut = availableQuantity <= 0,
                    CreatedAt = x.CreatedAt
                };
            }).ToList();

            var model = new FarmerProductsPageViewModel
            {
                Products = rows,
                TotalProducts = rows.Count,
                AvailableProducts = rows.Count(x =>
                    x.IsActive &&
                    x.IsAvailable &&
                    !x.IsSoldOut),
                SoldOutProducts = rows.Count(x => x.IsSoldOut),
                TotalAvailableQuantity =
                    rows.Sum(x => x.AvailableQuantity)
            };

            return View(model);
        }

        // =========================================================
        // ADD PRODUCT
        // SRS: Farmer enters product name, category, price, unit,
        // quantity, description and image.
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Create(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var model = new CreateFarmerProductViewModel();

            await PopulateSelectionsAsync(
                model,
                farmer.FarmerId,
                cancellationToken);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateFarmerProductViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            await ValidateMasterSelectionsAsync(
                model.CategoryId,
                model.UnitOfMeasureId,
                model.FarmerMarketId,
                farmer.FarmerId,
                cancellationToken);

            if (model.ImageFile != null)
            {
                ValidateImage(model.ImageFile);
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(
                    model,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // Product master row.
                // In this SRS-aligned flow, the farmer enters the name.
                var product = new Product
                {
                    CategoryId = model.CategoryId,
                    ProductName = model.ProductName.Trim(),
                    Description = model.Description ?? "",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.products.Add(product);

                await _context.SaveChangesAsync(cancellationToken);

                // Farmer-specific selling information.
                var farmerProduct = new FarmerProduct
                {
                    FarmerId = farmer.FarmerId,
                    ProductId = product.ProductId,
                    UnitOfMeasureId = model.UnitOfMeasureId,
                    Price = model.Price,
                    FarmerDescription = model.Description,
                    IsAvailable = model.IsAvailable,

                    // SRS requires admin approval of Farmer registration,
                    // not mandatory pre-approval of every product listing.
                    IsApproved = true,

                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.farmerproducts.Add(farmerProduct);

                await _context.SaveChangesAsync(cancellationToken);

                // Initial quantity available.
                var inventory = new Inventory
                {
                    FarmerProductId = farmerProduct.FarmerProductId,
                    FarmerMarketId = model.FarmerMarketId,
                    InventoryDate = DateTime.Today,
                    UnitPrice = model.Price,
                    StockQuantity = model.StockQuantity,
                    ReservedQuantity = 0,
                    SoldQuantity = 0,
                    IsSoldOut = model.StockQuantity <= 0,
                    IsAvailable =
                        model.IsAvailable &&
                        model.StockQuantity > 0,
                    UpdatedAt = DateTime.Now
                };

                _context.inventories.Add(inventory);

                if (model.ImageFile != null)
                {
                    var imageUrl =
                        await SaveImageAsync(model.ImageFile);

                    _context.productimages.Add(
                        new ProductImage
                        {
                            FarmerProductId =
                                farmerProduct.FarmerProductId,

                            ImageUrl = imageUrl,
                            AltText = model.ProductName,
                            IsPrimary = true,
                            SortOrder = 0
                        });
                }

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Product added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                ModelState.AddModelError(
                    "",
                    "Unable to add the product. Please try again.");

                await PopulateSelectionsAsync(
                    model,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }
        }

        // =========================================================
        // EDIT PRODUCT
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

            var listing = await _context.farmerproducts
                .AsNoTracking()
                .Include(fp => fp.Product)
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == id &&
                        fp.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (listing == null || listing.Product == null)
            {
                return NotFound();
            }

            var inventory = await _context.inventories
                .AsNoTracking()
                .Where(i => i.FarmerProductId == id)
                .OrderByDescending(i => i.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var model = new EditFarmerProductViewModel
            {
                FarmerProductId = listing.FarmerProductId,
                ProductName = listing.Product.ProductName,
                CategoryId = listing.Product.CategoryId,
                UnitOfMeasureId = listing.UnitOfMeasureId,
                FarmerMarketId =
                    inventory?.FarmerMarketId ?? 0,
                Price = listing.Price,
                StockQuantity =
                    inventory == null
                        ? 0
                        : Math.Max(
                            0,
                            inventory.StockQuantity -
                            inventory.ReservedQuantity -
                            inventory.SoldQuantity),
                Description =
                    listing.FarmerDescription
                    ?? listing.Product.Description,
                IsAvailable = listing.IsAvailable,

                ExistingImageUrl = await _context.productimages
                    .AsNoTracking()
                    .Where(pi => pi.FarmerProductId == id)
                    .OrderByDescending(pi => pi.IsPrimary)
                    .ThenBy(pi => pi.SortOrder)
                    .Select(pi => pi.ImageUrl)
                    .FirstOrDefaultAsync(cancellationToken)
            };

            await PopulateSelectionsAsync(
                model,
                farmer.FarmerId,
                cancellationToken);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditFarmerProductViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var listing = await _context.farmerproducts
                .Include(fp => fp.Product)
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId ==
                            model.FarmerProductId &&
                        fp.FarmerId ==
                            farmer.FarmerId,
                    cancellationToken);

            if (listing == null || listing.Product == null)
            {
                return NotFound();
            }

            await ValidateMasterSelectionsAsync(
                model.CategoryId,
                model.UnitOfMeasureId,
                model.FarmerMarketId,
                farmer.FarmerId,
                cancellationToken);

            if (model.ImageFile != null)
            {
                ValidateImage(model.ImageFile);
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(
                    model,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                listing.Product.ProductName =
                    model.ProductName.Trim();

                listing.Product.CategoryId =
                    model.CategoryId;

                listing.Product.Description =
                    model.Description ?? "";

                listing.UnitOfMeasureId =
                    model.UnitOfMeasureId;

                listing.Price = model.Price;

                listing.FarmerDescription =
                    model.Description;

                listing.IsAvailable =
                    model.IsAvailable;

                // Keep product live; admin can moderate/remove
                // inappropriate listings under the SRS.
                listing.IsApproved = true;

                var inventory = await _context.inventories
                    .Where(i =>
                        i.FarmerProductId ==
                            listing.FarmerProductId &&
                        i.FarmerMarketId ==
                            model.FarmerMarketId)
                    .OrderByDescending(i => i.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        FarmerProductId =
                            listing.FarmerProductId,

                        FarmerMarketId =
                            model.FarmerMarketId,

                        InventoryDate = DateTime.Today,

                        ReservedQuantity = 0,
                        SoldQuantity = 0
                    };

                    _context.inventories.Add(inventory);
                }

                // Do not erase already-reserved/sold quantities.
                inventory.UnitPrice = model.Price;

                inventory.StockQuantity =
                    model.StockQuantity +
                    inventory.ReservedQuantity +
                    inventory.SoldQuantity;

                inventory.IsSoldOut =
                    model.StockQuantity <= 0;

                inventory.IsAvailable =
                    model.IsAvailable &&
                    model.StockQuantity > 0;

                inventory.UpdatedAt = DateTime.Now;

                if (model.ImageFile != null)
                {
                    var imageUrl =
                        await SaveImageAsync(model.ImageFile);

                    var currentPrimary =
                        await _context.productimages
                            .FirstOrDefaultAsync(
                                pi =>
                                    pi.FarmerProductId ==
                                        listing.FarmerProductId &&
                                    pi.IsPrimary,
                                cancellationToken);

                    if (currentPrimary != null)
                    {
                        currentPrimary.IsPrimary = false;
                    }

                    _context.productimages.Add(
                        new ProductImage
                        {
                            FarmerProductId =
                                listing.FarmerProductId,

                            ImageUrl = imageUrl,
                            AltText = model.ProductName,
                            IsPrimary = true,
                            SortOrder = 0
                        });
                }

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Product updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                ModelState.AddModelError(
                    "",
                    "Unable to update the product.");

                await PopulateSelectionsAsync(
                    model,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }
        }

        // =========================================================
        // MARK AVAILABLE / UNAVAILABLE
        // =========================================================
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

            var listing = await _context.farmerproducts
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == id &&
                        fp.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (listing == null)
            {
                return NotFound();
            }

            listing.IsAvailable = !listing.IsAvailable;

            var inventories = await _context.inventories
                .Where(i => i.FarmerProductId == id)
                .ToListAsync(cancellationToken);

            foreach (var inventory in inventories)
            {
                var available =
                    inventory.StockQuantity -
                    inventory.ReservedQuantity -
                    inventory.SoldQuantity;

                inventory.IsAvailable =
                    listing.IsAvailable &&
                    available > 0;

                inventory.IsSoldOut =
                    available <= 0;

                inventory.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                listing.IsAvailable
                    ? "Product is now available."
                    : "Product is temporarily unavailable.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE/REMOVE FROM FARMER CATALOGUE
        // Soft delete keeps order history safe.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var listing = await _context.farmerproducts
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == id &&
                        fp.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (listing == null)
            {
                return NotFound();
            }

            listing.IsActive = false;
            listing.IsAvailable = false;

            var inventories = await _context.inventories
                .Where(i => i.FarmerProductId == id)
                .ToListAsync(cancellationToken);

            foreach (var inventory in inventories)
            {
                inventory.IsAvailable = false;
                inventory.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Product removed from your active catalogue.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPERS
        // =========================================================
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

        private async Task ValidateMasterSelectionsAsync(
            int categoryId,
            int unitId,
            int farmerMarketId,
            int farmerId,
            CancellationToken cancellationToken)
        {
            if (!await _context.categories
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.CategoryId == categoryId &&
                        c.IsActive,
                    cancellationToken))
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Please select a valid category.");
            }

            if (!await _context.unitofmeasures
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.UnitOfMeasureId == unitId &&
                        u.IsActive,
                    cancellationToken))
            {
                ModelState.AddModelError(
                    "UnitOfMeasureId",
                    "Please select a valid unit.");
            }

            if (!await _context.farmermarkets
                .AsNoTracking()
                .AnyAsync(
                    fm =>
                        fm.FarmerMarketId ==
                            farmerMarketId &&
                        fm.FarmerId == farmerId &&
                        fm.IsActive,
                    cancellationToken))
            {
                ModelState.AddModelError(
                    "FarmerMarketId",
                    "Please select one of your active markets.");
            }
        }

        private async Task PopulateSelectionsAsync(
            CreateFarmerProductViewModel model,
            int farmerId,
            CancellationToken cancellationToken)
        {
            model.Categories =
                await GetCategoryItemsAsync(cancellationToken);

            model.Units =
                await GetUnitItemsAsync(cancellationToken);

            model.Markets =
                await GetMarketItemsAsync(
                    farmerId,
                    cancellationToken);
        }

        private async Task PopulateSelectionsAsync(
            EditFarmerProductViewModel model,
            int farmerId,
            CancellationToken cancellationToken)
        {
            model.Categories =
                await GetCategoryItemsAsync(cancellationToken);

            model.Units =
                await GetUnitItemsAsync(cancellationToken);

            model.Markets =
                await GetMarketItemsAsync(
                    farmerId,
                    cancellationToken);
        }

        private async Task<List<SelectListItem>>
            GetCategoryItemsAsync(
                CancellationToken cancellationToken)
        {
            return await _context.categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<SelectListItem>>
            GetUnitItemsAsync(
                CancellationToken cancellationToken)
        {
            return await _context.unitofmeasures
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.UnitName)
                .Select(u => new SelectListItem
                {
                    Value =
                        u.UnitOfMeasureId.ToString(),

                    Text =
                        u.UnitName +
                        " (" +
                        u.UnitCode +
                        ")"
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<SelectListItem>>
            GetMarketItemsAsync(
                int farmerId,
                CancellationToken cancellationToken)
        {
            return await _context.farmermarkets
                .AsNoTracking()
                .Where(fm =>
                    fm.FarmerId == farmerId &&
                    fm.IsActive)
                .OrderBy(fm =>
                    fm.Market != null
                        ? fm.Market.MarketName
                        : "")
                .Select(fm => new SelectListItem
                {
                    Value =
                        fm.FarmerMarketId.ToString(),

                    Text =
                        fm.Market != null
                            ? fm.Market.MarketName
                            : "Market"
                })
                .ToListAsync(cancellationToken);
        }

        private void ValidateImage(IFormFile file)
        {
            if (file.Length <= 0)
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Image file is empty.");

                return;
            }

            if (file.Length > MaxImageBytes)
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Image must be 5 MB or smaller.");
            }

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }
        }

        private async Task<string> SaveImageAsync(
            IFormFile file)
        {
            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var relativeFolder =
                Path.Combine(
                    "uploads",
                    "farmer-products");

            var physicalFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    relativeFolder);

            Directory.CreateDirectory(physicalFolder);

            var physicalPath =
                Path.Combine(
                    physicalFolder,
                    fileName);

            await using var stream =
                System.IO.File.Create(physicalPath);

            await file.CopyToAsync(stream);

            return "/" +
                Path.Combine(
                    relativeFolder,
                    fileName)
                .Replace("\\", "/");
        }
    }
}
