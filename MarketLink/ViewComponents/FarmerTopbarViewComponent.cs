using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.ViewComponents
{
    public class FarmerTopbarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public FarmerTopbarViewComponent(
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
                return View(new FarmerTopbarViewModel());
            }

            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.UserId == userId);

            var user = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == userId);

            return View(new FarmerTopbarViewModel
            {
                FullName = user?.FullName ?? "Farmer",
                BusinessName =
                    farmer?.BusinessName ?? "Farmer",
                ProfileImageUrl =
                    farmer?.ProfileImageUrl
            });
        }
    }
}
