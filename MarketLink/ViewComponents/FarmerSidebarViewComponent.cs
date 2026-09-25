using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.ViewComponents
{
    public class FarmerSidebarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public FarmerSidebarViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdValue =
                UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? UserClaimsPrincipal.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return View(new FarmerSidebarViewModel());
            }

            var farmer = await _context.farmers
                .AsNoTracking()
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (farmer == null)
            {
                return View(new FarmerSidebarViewModel());
            }

            var productCount = await _context.farmerproducts
                .AsNoTracking()
                .CountAsync(fp =>
                    fp.FarmerId == farmer.FarmerId &&
                    fp.IsActive &&
                    fp.IsApproved);

            var pendingOrderCount = await _context.orders
                .AsNoTracking()
                .CountAsync(o =>
                    o.FarmerId == farmer.FarmerId &&
                    o.OrderStatus == "Placed");

            var unreadNotificationCount = await _context.notifications
                .AsNoTracking()
                .CountAsync(n =>
                    n.UserId == farmer.UserId &&
                    !n.IsRead);

            var model = new FarmerSidebarViewModel
            {
                FarmerId = farmer.FarmerId,
                BusinessName = farmer.BusinessName,
                FullName = farmer.User?.FullName ?? "",
                ProfileImageUrl = farmer.ProfileImageUrl,
                ProductCount = productCount,
                PendingOrderCount = pendingOrderCount,
                UnreadNotificationCount = unreadNotificationCount
            };

            return View(model);
        }
    }
}
