using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerNotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerNotificationsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? filter,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var query = _context.notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value);

            if (string.Equals(
                filter,
                "Unread",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(n => !n.IsRead);
            }
            else if (string.Equals(
                filter,
                "Read",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(n => n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            var allCounts = await _context.notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Unread = g.Count(x => !x.IsRead)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var rows = notifications
                .Select(n => new FarmerNotificationRowViewModel
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToList();

            return View(new FarmerNotificationsPageViewModel
            {
                Notifications = rows,
                TotalNotifications = allCounts?.Total ?? 0,
                UnreadNotifications = allCounts?.Unread ?? 0,
                ReadNotifications =
                    (allCounts?.Total ?? 0) -
                    (allCounts?.Unread ?? 0)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var notification = await _context.notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value,
                    cancellationToken);

            if (notification == null)
            {
                return NotFound();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var unread = await _context.notifications
                .Where(n =>
                    n.UserId == userId.Value &&
                    !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var item in unread)
            {
                item.IsRead = true;
                item.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "All notifications marked as read.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Open(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var notification = await _context.notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value,
                    cancellationToken);

            if (notification == null)
            {
                return NotFound();
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;

                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(notification.ActionUrl))
            {
                // Only allow local app URLs.
                if (Url.IsLocalUrl(notification.ActionUrl))
                {
                    return LocalRedirect(notification.ActionUrl);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Forbid();
            }

            var notification = await _context.notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value,
                    cancellationToken);

            if (notification == null)
            {
                return NotFound();
            }

            _context.notifications.Remove(notification);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Notification removed.";

            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
