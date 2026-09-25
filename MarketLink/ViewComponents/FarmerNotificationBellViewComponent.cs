using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.ViewComponents
{
    public class FarmerNotificationBellViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public FarmerNotificationBellViewComponent(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdValue =
                UserClaimsPrincipal.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? UserClaimsPrincipal.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return View(
                    new FarmerNotificationBellViewModel());
            }

            var unreadCount = await _context.notifications
                .AsNoTracking()
                .CountAsync(n =>
                    n.UserId == userId &&
                    !n.IsRead);

            return View(
                new FarmerNotificationBellViewModel
                {
                    UnreadCount = unreadCount
                });
        }
    }
}
