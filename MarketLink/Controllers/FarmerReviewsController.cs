using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerReviewsController(ApplicationDbContext context)
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

            var farmerProductIds = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => fp.FarmerId == farmer.FarmerId)
                .Select(fp => fp.FarmerProductId)
                .ToListAsync(cancellationToken);

            var reviews = await _context.reviews
                .AsNoTracking()
                .Where(r =>
                    r.IsVisible &&
                    (
                        r.FarmerId == farmer.FarmerId ||
                        (
                            r.FarmerProductId.HasValue &&
                            farmerProductIds.Contains(
                                r.FarmerProductId.Value)
                        )
                    ))
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync(cancellationToken);

            var reviewIds = reviews
                .Select(r => r.ReviewId)
                .ToList();

            var responses = await _context.reviewresponses
                .AsNoTracking()
                .Where(rr =>
                    rr.FarmerId == farmer.FarmerId &&
                    reviewIds.Contains(rr.ReviewId))
                .ToListAsync(cancellationToken);

            var customerIds = reviews
                .Select(r => r.CustomerId)
                .Distinct()
                .ToList();

            var customers = await _context.customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync(cancellationToken);

            var customerUserIds = customers
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var users = await _context.users
                .AsNoTracking()
                .Where(u => customerUserIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, cancellationToken);

            var customerMap = customers
                .ToDictionary(c => c.CustomerId);

            var orderIds = reviews
                .Select(r => r.OrderId)
                .Distinct()
                .ToList();

            var orders = await _context.orders
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.OrderId))
                .ToDictionaryAsync(o => o.OrderId, cancellationToken);

            var targetFarmerProductIds = reviews
                .Where(r => r.FarmerProductId.HasValue)
                .Select(r => r.FarmerProductId!.Value)
                .Distinct()
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp =>
                    targetFarmerProductIds.Contains(fp.FarmerProductId))
                .ToListAsync(cancellationToken);

            var productIds = farmerProducts
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId, cancellationToken);

            var farmerProductMap = farmerProducts
                .ToDictionary(fp => fp.FarmerProductId);

            var rows = reviews.Select(r =>
            {
                customerMap.TryGetValue(r.CustomerId, out var customer);

                User? customerUser = null;

                if (customer != null)
                {
                    users.TryGetValue(customer.UserId, out customerUser);
                }

                orders.TryGetValue(r.OrderId, out var order);

                var targetName = "Farmer";

                if (r.FarmerProductId.HasValue &&
                    farmerProductMap.TryGetValue(
                        r.FarmerProductId.Value,
                        out var fp) &&
                    products.TryGetValue(
                        fp.ProductId,
                        out var product))
                {
                    targetName = product.ProductName;
                }

                var response = responses
                    .FirstOrDefault(rr => rr.ReviewId == r.ReviewId);

                return new FarmerReviewRowViewModel
                {
                    ReviewId = r.ReviewId,
                    CustomerName =
                        customerUser?.FullName ?? "Customer",
                    OrderNo =
                        order?.OrderNo ?? $"Order #{r.OrderId}",
                    TargetName = targetName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate,
                    ResponseText = response?.ResponseText,
                    RespondedAt = response?.RespondedAt
                };
            }).ToList();

            return View(new FarmerReviewsPageViewModel
            {
                Reviews = rows,
                TotalReviews = rows.Count,
                AverageRating =
                    rows.Count == 0
                        ? 0
                        : rows.Average(x => x.Rating),
                UnansweredReviews =
                    rows.Count(x =>
                        string.IsNullOrWhiteSpace(x.ResponseText))
            });
        }

        [HttpGet]
        public async Task<IActionResult> Respond(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var review = await GetOwnedReviewAsync(
                id,
                farmer.FarmerId,
                cancellationToken);

            if (review == null)
            {
                return NotFound();
            }

            var response = await _context.reviewresponses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    rr =>
                        rr.ReviewId == id &&
                        rr.FarmerId == farmer.FarmerId,
                    cancellationToken);

            var customer = await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CustomerId == review.CustomerId,
                    cancellationToken);

            User? customerUser = null;

            if (customer != null)
            {
                customerUser = await _context.users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.UserId == customer.UserId,
                        cancellationToken);
            }

            var order = await _context.orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.OrderId == review.OrderId,
                    cancellationToken);

            return View(new FarmerReviewResponseViewModel
            {
                ReviewId = review.ReviewId,
                CustomerName =
                    customerUser?.FullName ?? "Customer",
                OrderNo =
                    order?.OrderNo ?? $"Order #{review.OrderId}",
                Rating = review.Rating,
                Comment = review.Comment,
                ResponseText =
                    response?.ResponseText ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(
            FarmerReviewResponseViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var review = await GetOwnedReviewAsync(
                model.ReviewId,
                farmer.FarmerId,
                cancellationToken);

            if (review == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var customer = await _context.customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.CustomerId == review.CustomerId,
                        cancellationToken);

                User? customerUser = null;

                if (customer != null)
                {
                    customerUser = await _context.users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            u => u.UserId == customer.UserId,
                            cancellationToken);
                }

                var order = await _context.orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        o => o.OrderId == review.OrderId,
                        cancellationToken);

                model.CustomerName =
                    customerUser?.FullName ?? "Customer";

                model.OrderNo =
                    order?.OrderNo ?? $"Order #{review.OrderId}";

                model.Rating = review.Rating;
                model.Comment = review.Comment;

                return View(model);
            }

            var response = await _context.reviewresponses
                .FirstOrDefaultAsync(
                    rr =>
                        rr.ReviewId == review.ReviewId &&
                        rr.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (response == null)
            {
                response = new ReviewResponse
                {
                    ReviewId = review.ReviewId,
                    FarmerId = farmer.FarmerId
                };

                _context.reviewresponses.Add(response);
            }

            response.ResponseText =
                model.ResponseText.Trim();

            response.RespondedAt =
                DateTime.Now;

            var customerForNotification = await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CustomerId == review.CustomerId,
                    cancellationToken);

            if (customerForNotification != null)
            {
                _context.notifications.Add(
                    new Notification
                    {
                        UserId =
                            customerForNotification.UserId,
                        Title =
                            "Farmer Replied to Your Review",
                        Message =
                            "A farmer has responded to your review.",
                        NotificationType =
                            "ReviewResponse",
                        ActionUrl =
                            $"/CustomerOrders/Details/{review.OrderId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Your review response has been saved.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<Review?> GetOwnedReviewAsync(
            int reviewId,
            int farmerId,
            CancellationToken cancellationToken)
        {
            var farmerProductIds = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => fp.FarmerId == farmerId)
                .Select(fp => fp.FarmerProductId)
                .ToListAsync(cancellationToken);

            return await _context.reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r =>
                        r.ReviewId == reviewId &&
                        r.IsVisible &&
                        (
                            r.FarmerId == farmerId ||
                            (
                                r.FarmerProductId.HasValue &&
                                farmerProductIds.Contains(
                                    r.FarmerProductId.Value)
                            )
                        ),
                    cancellationToken);
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
