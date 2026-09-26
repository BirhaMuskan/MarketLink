using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.ViewComponents
{
    public class AnnouncementBannerViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementBannerViewComponent(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!(UserClaimsPrincipal.Identity?.IsAuthenticated ?? false))
            {
                return View(new AnnouncementBannerViewModel());
            }

            var role =
                UserClaimsPrincipal.FindFirstValue(ClaimTypes.Role)
                ?? "";

            var audience =
                role.Equals("Customer", StringComparison.OrdinalIgnoreCase)
                    ? "CUSTOMER"
                    : role.Equals("Farmer", StringComparison.OrdinalIgnoreCase)
                        ? "FARMER"
                        : "";

            if (string.IsNullOrWhiteSpace(audience))
                return View(new AnnouncementBannerViewModel());

            var now = DateTime.Now;

            var items = await _context.announcements
                .AsNoTracking()
                .Where(a =>
                    a.IsActive &&
                    a.PublishFrom <= now &&
                    (!a.PublishUntil.HasValue ||
                     a.PublishUntil.Value >= now) &&
                    (a.Audience == "ALL" ||
                     a.Audience == audience))
                .OrderByDescending(a => a.PublishFrom)
                .Take(3)
                .Select(a => new AnnouncementBannerItemViewModel
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Message = a.Message,
                    Audience = a.Audience
                })
                .ToListAsync();

            return View(new AnnouncementBannerViewModel
            {
                Items = items
            });
        }
    }
}
