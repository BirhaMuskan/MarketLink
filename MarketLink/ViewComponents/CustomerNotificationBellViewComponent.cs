using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.ViewComponents
{
    public class CustomerNotificationBellViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CustomerNotificationBellViewComponent(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var value =
                UserClaimsPrincipal.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? UserClaimsPrincipal.FindFirstValue("sub");

            if (!int.TryParse(value, out var userId))
            {
                return View(
                    new CustomerNotificationBellViewModel());
            }

            var unreadCount = await _context.notifications
                .AsNoTracking()
                .CountAsync(n =>
                    n.UserId == userId &&
                    !n.IsRead);

            return View(
                new CustomerNotificationBellViewModel
                {
                    UnreadCount = unreadCount
                });
        }
    }
}
