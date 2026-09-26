using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerReviewsController(ApplicationDbContext context)
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

            var completedOrders = await _context.orders
                .AsNoTracking()
                .Where(o =>
                    o.CustomerId == customer.CustomerId &&
                    o.OrderStatus == "Completed")
                .OrderByDescending(o => o.CompletedAt ?? o.OrderDate)
                .ToListAsync(cancellationToken);

            var orderIds = completedOrders
                .Select(o => o.OrderId)
                .ToList();

            var orderItems = await _context.orderitems
                .AsNoTracking()
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync(cancellationToken);

            var farmerIds = completedOrders
                .Select(o => o.FarmerId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var farmerProductIds = orderItems
                .Select(oi => oi.FarmerProductId)
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

            var reviews = await _context.reviews
                .AsNoTracking()
                .Where(r =>
                    r.CustomerId == customer.CustomerId &&
                    orderIds.Contains(r.OrderId))
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync(cancellationToken);

            var reviewIds = reviews
                .Select(r => r.ReviewId)
                .ToList();

            var responses = await _context.reviewresponses
                .AsNoTracking()
                .Where(rr => reviewIds.Contains(rr.ReviewId))
                .ToListAsync(cancellationToken);

            var orderRows = completedOrders.Select(order =>
            {
                farmers.TryGetValue(order.FarmerId, out var farmer);

                var items = orderItems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .GroupBy(oi => oi.FarmerProductId)
                    .Select(g =>
                    {
                        var farmerProductId = g.Key;
                        var productName = g.First().ProductName;

                        return new CustomerReviewProductTargetViewModel
                        {
                            FarmerProductId = farmerProductId,
                            ProductName = productName,
                            Reviewed = reviews.Any(r =>
                                r.OrderId == order.OrderId &&
                                r.FarmerProductId == farmerProductId)
                        };
                    })
                    .OrderBy(x => x.ProductName)
                    .ToList();

                return new CustomerReviewOrderViewModel
                {
                    OrderId = order.OrderId,
                    OrderNo = order.OrderNo,
                    FarmerId = order.FarmerId,
                    FarmerName = farmer?.BusinessName ?? "Farmer",
                    PickupDate = order.PickupDate,
                    FarmerReviewed = reviews.Any(r =>
                        r.OrderId == order.OrderId &&
                        r.FarmerId == order.FarmerId &&
                        r.FarmerProductId == null),
                    Products = items
                };
            }).ToList();

            var reviewHistory = reviews.Select(review =>
            {
                string targetName;

                if (review.FarmerProductId.HasValue &&
                    farmerProducts.TryGetValue(
                        review.FarmerProductId.Value,
                        out var fp) &&
                    products.TryGetValue(
                        fp.ProductId,
                        out var product))
                {
                    targetName = product.ProductName;
                }
                else if (review.FarmerId.HasValue &&
                         farmers.TryGetValue(
                             review.FarmerId.Value,
                             out var farmer))
                {
                    targetName = farmer.BusinessName;
                }
                else
                {
                    targetName = "Review";
                }

                var response = responses
                    .FirstOrDefault(rr => rr.ReviewId == review.ReviewId);

                var orderNo = completedOrders
                    .FirstOrDefault(o => o.OrderId == review.OrderId)
                    ?.OrderNo ?? "";

                return new CustomerReviewHistoryViewModel
                {
                    ReviewId = review.ReviewId,
                    TargetName = targetName,
                    OrderNo = orderNo,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    ReviewDate = review.ReviewDate,
                    FarmerResponse = response?.ResponseText,
                    RespondedAt = response?.RespondedAt
                };
            }).ToList();

            return View(new CustomerReviewsPageViewModel
            {
                CompletedOrders = completedOrders.Count,
                ReviewsGiven = reviews.Count,
                Orders = orderRows,
                Reviews = reviewHistory
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create(
            int orderId,
            int? farmerId,
            int? farmerProductId,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var validation = await ValidateTargetAsync(
                customer.CustomerId,
                orderId,
                farmerId,
                farmerProductId,
                cancellationToken);

            if (!validation.Valid)
                return BadRequest(validation.ErrorMessage);

            var duplicate = await ReviewAlreadyExistsAsync(
                customer.CustomerId,
                orderId,
                farmerId,
                farmerProductId,
                cancellationToken);

            if (duplicate)
            {
                TempData["ErrorMessage"] =
                    "You already reviewed this item for this order.";

                return RedirectToAction(nameof(Index));
            }

            return View(new CustomerCreateReviewViewModel
            {
                OrderId = orderId,
                FarmerId = farmerId,
                FarmerProductId = farmerProductId,
                TargetName = validation.TargetName,
                OrderNo = validation.OrderNo,
                Rating = 5
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CustomerCreateReviewViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var validation = await ValidateTargetAsync(
                customer.CustomerId,
                model.OrderId,
                model.FarmerId,
                model.FarmerProductId,
                cancellationToken);

            if (!validation.Valid)
            {
                ModelState.AddModelError("", validation.ErrorMessage);
            }

            var duplicate = await ReviewAlreadyExistsAsync(
                customer.CustomerId,
                model.OrderId,
                model.FarmerId,
                model.FarmerProductId,
                cancellationToken);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "",
                    "You already reviewed this item for this order.");
            }

            if (!ModelState.IsValid)
            {
                model.TargetName = validation.TargetName;
                model.OrderNo = validation.OrderNo;

                return View(model);
            }

            var review = new Review
            {
                CustomerId = customer.CustomerId,
                OrderId = model.OrderId,
                FarmerId = model.FarmerId,
                FarmerProductId = model.FarmerProductId,
                Rating = model.Rating,
                Comment = model.Comment.Trim(),
                IsVisible = true,
                ReviewDate = DateTime.Now
            };

            _context.reviews.Add(review);

            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FarmerId == validation.FarmerId,
                    cancellationToken);

            if (farmer != null)
            {
                _context.notifications.Add(
                    new Notification
                    {
                        UserId = farmer.UserId,
                        Title = "New Customer Review",
                        Message =
                            $"A customer left a {model.Rating}-star review for {validation.TargetName}.",
                        NotificationType = "Review",
                        ActionUrl = "/FarmerReviews",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Thank you. Your review has been submitted.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var review = await _context.reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r =>
                        r.ReviewId == id &&
                        r.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (review == null)
                return NotFound();

            var targetName = await GetTargetNameAsync(
                review.FarmerId,
                review.FarmerProductId,
                cancellationToken);

            var order = await _context.orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == review.OrderId &&
                        o.CustomerId == customer.CustomerId,
                    cancellationToken);

            return View(new CustomerEditReviewViewModel
            {
                ReviewId = review.ReviewId,
                TargetName = targetName,
                OrderNo = order?.OrderNo ?? "",
                Rating = review.Rating,
                Comment = review.Comment
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            CustomerEditReviewViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var review = await _context.reviews
                .FirstOrDefaultAsync(
                    r =>
                        r.ReviewId == model.ReviewId &&
                        r.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (review == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.TargetName = await GetTargetNameAsync(
                    review.FarmerId,
                    review.FarmerProductId,
                    cancellationToken);

                var order = await _context.orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        o => o.OrderId == review.OrderId,
                        cancellationToken);

                model.OrderNo = order?.OrderNo ?? "";

                return View(model);
            }

            review.Rating = model.Rating;
            review.Comment = model.Comment.Trim();
            review.ReviewDate = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Your review has been updated.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ReviewAlreadyExistsAsync(
            int customerId,
            int orderId,
            int? farmerId,
            int? farmerProductId,
            CancellationToken cancellationToken)
        {
            return await _context.reviews
                .AsNoTracking()
                .AnyAsync(
                    r =>
                        r.CustomerId == customerId &&
                        r.OrderId == orderId &&
                        r.FarmerId == farmerId &&
                        r.FarmerProductId == farmerProductId,
                    cancellationToken);
        }

        private async Task<(
            bool Valid,
            string ErrorMessage,
            string TargetName,
            string OrderNo,
            int FarmerId)> ValidateTargetAsync(
            int customerId,
            int orderId,
            int? farmerId,
            int? farmerProductId,
            CancellationToken cancellationToken)
        {
            var order = await _context.orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == orderId &&
                        o.CustomerId == customerId &&
                        o.OrderStatus == "Completed",
                    cancellationToken);

            if (order == null)
            {
                return (
                    false,
                    "Reviews are only available for your completed orders.",
                    "",
                    "",
                    0);
            }

            if (farmerProductId.HasValue)
            {
                var belongsToOrder = await _context.orderitems
                    .AsNoTracking()
                    .AnyAsync(
                        oi =>
                            oi.OrderId == order.OrderId &&
                            oi.FarmerProductId == farmerProductId.Value,
                        cancellationToken);

                if (!belongsToOrder)
                {
                    return (
                        false,
                        "This product was not part of the selected order.",
                        "",
                        order.OrderNo,
                        order.FarmerId);
                }

                var fp = await _context.farmerproducts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.FarmerProductId == farmerProductId.Value &&
                            x.FarmerId == order.FarmerId,
                        cancellationToken);

                if (fp == null)
                {
                    return (
                        false,
                        "The product listing is unavailable.",
                        "",
                        order.OrderNo,
                        order.FarmerId);
                }

                var product = await _context.products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.ProductId == fp.ProductId,
                        cancellationToken);

                return (
                    true,
                    "",
                    product?.ProductName ?? "Product",
                    order.OrderNo,
                    order.FarmerId);
            }

            if (farmerId.HasValue &&
                farmerId.Value == order.FarmerId)
            {
                var farmer = await _context.farmers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        f => f.FarmerId == farmerId.Value,
                        cancellationToken);

                return (
                    true,
                    "",
                    farmer?.BusinessName ?? "Farmer",
                    order.OrderNo,
                    order.FarmerId);
            }

            return (
                false,
                "Choose either the order farmer or a product from the completed order.",
                "",
                order.OrderNo,
                order.FarmerId);
        }

        private async Task<string> GetTargetNameAsync(
            int? farmerId,
            int? farmerProductId,
            CancellationToken cancellationToken)
        {
            if (farmerProductId.HasValue)
            {
                var fp = await _context.farmerproducts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.FarmerProductId ==
                            farmerProductId.Value,
                        cancellationToken);

                if (fp != null)
                {
                    var product = await _context.products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            p => p.ProductId == fp.ProductId,
                            cancellationToken);

                    return product?.ProductName ?? "Product";
                }
            }

            if (farmerId.HasValue)
            {
                var farmer = await _context.farmers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        f => f.FarmerId == farmerId.Value,
                        cancellationToken);

                return farmer?.BusinessName ?? "Farmer";
            }

            return "Review";
        }

        private async Task<Customer?> GetCurrentCustomerAsync(
            CancellationToken cancellationToken)
        {
            var value =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(value, out var userId))
                return null;

            return await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId,
                    cancellationToken);
        }
    }
}
