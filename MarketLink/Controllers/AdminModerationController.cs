using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminModerationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminModerationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tab,
            string? search,
            CancellationToken cancellationToken)
        {
            var currentTab =
                string.Equals(tab, "Reviews", StringComparison.OrdinalIgnoreCase)
                    ? "Reviews"
                    : "Products";

            var productRows = await BuildProductsAsync(cancellationToken);
            var reviewRows = await BuildReviewsAsync(cancellationToken);

            var totalProducts = productRows.Count;
            var hiddenProducts = productRows.Count(x => !x.IsActive);

            var totalReviews = reviewRows.Count;
            var hiddenReviews = reviewRows.Count(x => !x.IsVisible);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();

                productRows = productRows
                    .Where(x =>
                        x.ProductName.ToLower().Contains(term) ||
                        x.FarmerName.ToLower().Contains(term) ||
                        x.CategoryName.ToLower().Contains(term))
                    .ToList();

                reviewRows = reviewRows
                    .Where(x =>
                        x.CustomerName.ToLower().Contains(term) ||
                        x.TargetName.ToLower().Contains(term) ||
                        x.Comment.ToLower().Contains(term) ||
                        x.OrderNo.ToLower().Contains(term))
                    .ToList();
            }

            return View(new AdminModerationPageViewModel
            {
                Tab = currentTab,
                Search = search ?? "",

                TotalProducts = totalProducts,
                HiddenProducts = hiddenProducts,
                TotalReviews = totalReviews,
                HiddenReviews = hiddenReviews,

                Products = productRows,
                Reviews = reviewRows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProduct(
            int id,
            string? returnTab,
            CancellationToken cancellationToken)
        {
            var farmerProduct = await _context.farmerproducts
                .FirstOrDefaultAsync(
                    fp => fp.FarmerProductId == id,
                    cancellationToken);

            if (farmerProduct == null)
                return NotFound();

            farmerProduct.IsActive = !farmerProduct.IsActive;

            if (!farmerProduct.IsActive)
                farmerProduct.IsAvailable = false;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                farmerProduct.IsActive
                    ? "Product listing restored."
                    : "Product listing hidden from customers.";

            return RedirectToAction(
                nameof(Index),
                new { tab = returnTab ?? "Products" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReview(
            int id,
            string? returnTab,
            CancellationToken cancellationToken)
        {
            var review = await _context.reviews
                .FirstOrDefaultAsync(
                    r => r.ReviewId == id,
                    cancellationToken);

            if (review == null)
                return NotFound();

            review.IsVisible = !review.IsVisible;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                review.IsVisible
                    ? "Review restored."
                    : "Review hidden from customers.";

            return RedirectToAction(
                nameof(Index),
                new { tab = returnTab ?? "Reviews" });
        }

        private async Task<List<AdminModerationProductViewModel>>
            BuildProductsAsync(CancellationToken cancellationToken)
        {
            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .OrderByDescending(fp => fp.CreatedAt)
                .ToListAsync(cancellationToken);

            var farmerIds = farmerProducts
                .Select(fp => fp.FarmerId)
                .Distinct()
                .ToList();

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var unitIds = farmerProducts
                .Select(fp => fp.UnitOfMeasureId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var products = await _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync(cancellationToken);

            var categoryIds = products
                .Select(p => p.CategoryId)
                .Distinct()
                .ToList();

            var categories = await _context.categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.CategoryId))
                .ToDictionaryAsync(
                    c => c.CategoryId,
                    cancellationToken);

            var productMap = products.ToDictionary(p => p.ProductId);

            var units = await _context.unitofmeasures
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.UnitOfMeasureId))
                .ToDictionaryAsync(
                    u => u.UnitOfMeasureId,
                    cancellationToken);

            return farmerProducts
                .Select(fp =>
                {
                    farmers.TryGetValue(fp.FarmerId, out var farmer);
                    productMap.TryGetValue(fp.ProductId, out var product);
                    units.TryGetValue(fp.UnitOfMeasureId, out var unit);

                    string categoryName = "Category";

                    if (product != null &&
                        categories.TryGetValue(
                            product.CategoryId,
                            out var category))
                    {
                        categoryName = category.CategoryName;
                    }

                    return new AdminModerationProductViewModel
                    {
                        FarmerProductId = fp.FarmerProductId,
                        FarmerId = fp.FarmerId,
                        ProductName = product?.ProductName ?? "Product",
                        FarmerName = farmer?.BusinessName ?? "Farmer",
                        CategoryName = categoryName,
                        UnitName = unit?.UnitCode ?? unit?.UnitName ?? "",
                        Price = fp.Price,
                        FarmerDescription = fp.FarmerDescription,
                        IsApproved = fp.IsApproved,
                        IsActive = fp.IsActive,
                        IsAvailable = fp.IsAvailable,
                        CreatedAt = fp.CreatedAt
                    };
                })
                .ToList();
        }

        private async Task<List<AdminModerationReviewViewModel>>
            BuildReviewsAsync(CancellationToken cancellationToken)
        {
            var reviews = await _context.reviews
                .AsNoTracking()
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync(cancellationToken);

            var customerIds = reviews
                .Select(r => r.CustomerId)
                .Distinct()
                .ToList();

            var customers = await _context.customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToDictionaryAsync(
                    c => c.CustomerId,
                    cancellationToken);

            var userIds = customers.Values
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var users = await _context.users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(
                    u => u.UserId,
                    cancellationToken);

            var orderIds = reviews
                .Select(r => r.OrderId)
                .Distinct()
                .ToList();

            var orders = await _context.orders
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.OrderId))
                .ToDictionaryAsync(
                    o => o.OrderId,
                    cancellationToken);

            var farmerIds = reviews
                .Where(r => r.FarmerId.HasValue)
                .Select(r => r.FarmerId!.Value)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var farmerProductIds = reviews
                .Where(r => r.FarmerProductId.HasValue)
                .Select(r => r.FarmerProductId!.Value)
                .Distinct()
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => farmerProductIds.Contains(fp.FarmerProductId))
                .ToDictionaryAsync(
                    fp => fp.FarmerProductId,
                    cancellationToken);

            var productIds = farmerProducts.Values
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(
                    p => p.ProductId,
                    cancellationToken);

            return reviews
                .Select(r =>
                {
                    string customerName = "Customer";
                    if (customers.TryGetValue(r.CustomerId, out var customer) &&
                        users.TryGetValue(customer.UserId, out var user))
                    {
                        customerName = user.FullName;
                    }

                    string targetName = "Review";

                    if (r.FarmerProductId.HasValue &&
                        farmerProducts.TryGetValue(
                            r.FarmerProductId.Value,
                            out var farmerProduct) &&
                        products.TryGetValue(
                            farmerProduct.ProductId,
                            out var product))
                    {
                        targetName = product.ProductName;
                    }
                    else if (r.FarmerId.HasValue &&
                             farmers.TryGetValue(
                                 r.FarmerId.Value,
                                 out var farmer))
                    {
                        targetName = farmer.BusinessName;
                    }

                    var orderNo =
                        orders.TryGetValue(r.OrderId, out var order)
                            ? order.OrderNo
                            : "";

                    return new AdminModerationReviewViewModel
                    {
                        ReviewId = r.ReviewId,
                        OrderId = r.OrderId,
                        CustomerName = customerName,
                        TargetName = targetName,
                        OrderNo = orderNo,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        IsVisible = r.IsVisible,
                        ReviewDate = r.ReviewDate
                    };
                })
                .ToList();
        }
    }
}
