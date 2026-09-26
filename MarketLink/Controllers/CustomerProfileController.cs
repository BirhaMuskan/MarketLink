using System.Security.Claims;
using MarketLink.Models;
using MarketLink.Services;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

        public CustomerProfileController(
            ApplicationDbContext context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(
                cancellationToken);

            if (customer == null)
                return Forbid();

            var user = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == customer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            return View(
                await BuildViewModelAsync(
                    customer,
                    user,
                    cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            CustomerProfileViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(
                cancellationToken);

            if (customer == null)
                return Forbid();

            var user = await _context.users
                .FirstOrDefaultAsync(
                    u => u.UserId == customer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            model.CustomerId = customer.CustomerId;
            model.UserId = user.UserId;

            var normalizedEmail =
                (model.Email ?? "")
                    .Trim()
                    .ToLowerInvariant();

            var duplicateEmail = await _context.users
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.UserId != user.UserId &&
                        u.Email.ToLower() == normalizedEmail,
                    cancellationToken);

            if (duplicateEmail)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Another account already uses this email address.");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel =
                    await BuildViewModelAsync(
                        customer,
                        user,
                        cancellationToken);

                invalidModel.FullName = model.FullName;
                invalidModel.Email = model.Email;
                invalidModel.Phone = model.Phone;
                invalidModel.Address = model.Address;

                return View("Index", invalidModel);
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.Phone = (model.Phone ?? "").Trim();
            user.Address = (model.Address ?? "").Trim();

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            TempData["ProfileRefreshMessage"] =
                "If your dashboard header still shows the previous name or email, sign out and sign in again so the JWT claims refresh.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangeCustomerPasswordViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(
                cancellationToken);

            if (customer == null)
                return Forbid();

            var user = await _context.users
                .FirstOrDefaultAsync(
                    u => u.UserId == customer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] =
                    string.Join(
                        " ",
                        ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .Where(m => !string.IsNullOrWhiteSpace(m)));

                return RedirectToAction(nameof(Index));
            }

            if (!_passwordService.VerifyPassword(
                model.CurrentPassword,
                user.PasswordHash))
            {
                TempData["PasswordError"] =
                    "Current password is incorrect.";

                return RedirectToAction(nameof(Index));
            }

            if (model.CurrentPassword ==
                model.NewPassword)
            {
                TempData["PasswordError"] =
                    "New password must be different from your current password.";

                return RedirectToAction(nameof(Index));
            }

            user.PasswordHash =
                _passwordService.HashPassword(
                    model.NewPassword);

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["PasswordSuccess"] =
                "Password changed successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<CustomerProfileViewModel>
            BuildViewModelAsync(
                Customer customer,
                User user,
                CancellationToken cancellationToken)
        {
            var totalOrders =
                await _context.orders
                    .AsNoTracking()
                    .CountAsync(
                        o =>
                            o.CustomerId ==
                            customer.CustomerId,
                        cancellationToken);

            var completedOrders =
                await _context.orders
                    .AsNoTracking()
                    .CountAsync(
                        o =>
                            o.CustomerId ==
                            customer.CustomerId &&
                            o.OrderStatus ==
                            "Completed",
                        cancellationToken);

            var favoriteProducts =
                await _context.favoriteproducts
                    .AsNoTracking()
                    .CountAsync(
                        fp =>
                            fp.CustomerId ==
                            customer.CustomerId,
                        cancellationToken);

            return new CustomerProfileViewModel
            {
                CustomerId = customer.CustomerId,
                UserId = user.UserId,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,

                AccountCreatedAt = user.CreatedAt,
                CustomerSince = customer.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,

                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                FavoriteProducts = favoriteProducts
            };
        }

        private async Task<Customer?>
            GetCurrentCustomerAsync(
                CancellationToken cancellationToken)
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
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
