using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerNotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerNotificationsController(ApplicationDbContext context)
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
                return Forbid();

            var currentFilter =
                string.IsNullOrWhiteSpace(filter)
                    ? "All"
                    : filter.Trim();

            var query = _context.notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value);

            if (currentFilter == "Unread")
                query = query.Where(n => !n.IsRead);
            else if (currentFilter == "Read")
                query = query.Where(n => n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            var all = await _context.notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value)
                .ToListAsync(cancellationToken);

            return View(new CustomerNotificationsPageViewModel
            {
                CurrentFilter = currentFilter,

                TotalNotifications = all.Count,
                UnreadNotifications = all.Count(n => !n.IsRead),
                ReadNotifications = all.Count(n => n.IsRead),

                Notifications = notifications
                    .Select(n => new CustomerNotificationRowViewModel
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
                    .ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Open(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return Forbid();

            var notification = await _context.notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value,
                    cancellationToken);

            if (notification == null)
                return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;

                await _context.SaveChangesAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(notification.ActionUrl) &&
                Url.IsLocalUrl(notification.ActionUrl))
            {
                return LocalRedirect(notification.ActionUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return Forbid();

            var notification = await _context.notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value,
                    cancellationToken);

            if (notification == null)
                return NotFound();

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
                return Forbid();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return Forbid();

            var notification = await _context.notifications
                .FirstOrDefaultAsync(
                    n =>
                        n.NotificationId == id &&
                        n.UserId == userId.Value,
                    cancellationToken);

            if (notification == null)
                return NotFound();

            _context.notifications.Remove(notification);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Notification removed.";

            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return int.TryParse(value, out var userId)
                ? userId
                : null;
        }
    }
}
