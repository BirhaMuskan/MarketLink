using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public FarmerProfileController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
                return NotFound();

            var user = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == farmer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            return View(new FarmerProfileMapViewModel
            {
                FarmerId = farmer.FarmerId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                BusinessName = farmer.BusinessName,
                Description = farmer.Description,
                Address = farmer.Address,
                Latitude = farmer.Latitude,
                Longitude = farmer.Longitude,
                ProfileImageUrl = farmer.ProfileImageUrl,
                IsApproved = farmer.IsApproved,
                IsActive = farmer.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            FarmerProfileMapViewModel model,
            CancellationToken cancellationToken)
        {
            // Load tracked Farmer directly in POST.
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
                return Forbid();

            var farmer = await _context.farmers
                .FirstOrDefaultAsync(
                    f =>
                        f.UserId == userId &&
                        f.FarmerId == model.FarmerId,
                    cancellationToken);

            if (farmer == null)
                return NotFound();

            var user = await _context.users
                .FirstOrDefaultAsync(
                    u => u.UserId == farmer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            // Values not editable on this page.
            model.Email = user.Email;
            model.IsApproved = farmer.IsApproved;
            model.IsActive = farmer.IsActive;
            model.ProfileImageUrl = farmer.ProfileImageUrl;

            if (model.Latitude.HasValue &&
                (model.Latitude.Value < -90 ||
                 model.Latitude.Value > 90))
            {
                ModelState.AddModelError(
                    nameof(model.Latitude),
                    "Latitude must be between -90 and 90.");
            }

            if (model.Longitude.HasValue &&
                (model.Longitude.Value < -180 ||
                 model.Longitude.Value > 180))
            {
                ModelState.AddModelError(
                    nameof(model.Longitude),
                    "Longitude must be between -180 and 180.");
            }

            if (model.ProfileImageFile != null)
            {
                var extension =
                    Path.GetExtension(model.ProfileImageFile.FileName)
                        .ToLowerInvariant();

                string[] allowed =
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (!allowed.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.ProfileImageFile),
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }

                if (model.ProfileImageFile.Length <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.ProfileImageFile),
                        "The selected image is empty.");
                }

                if (model.ProfileImageFile.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(model.ProfileImageFile),
                        "Profile image must be 5 MB or smaller.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update User fields.
            user.FullName = model.FullName.Trim();
            user.Phone = model.Phone?.Trim() ?? "";
            user.Address = model.Address?.Trim() ?? "";

            // Update Farmer fields.
            farmer.BusinessName = model.BusinessName.Trim();
            farmer.Description = model.Description?.Trim() ?? "";
            farmer.Address = model.Address?.Trim() ?? "";
            farmer.Latitude = model.Latitude;
            farmer.Longitude = model.Longitude;

            // Save image first, then store its web URL on Farmer.
            if (model.ProfileImageFile != null)
            {
                farmer.ProfileImageUrl =
                    await SaveProfileImageAsync(
                        model.ProfileImageFile,
                        farmer.FarmerId,
                        cancellationToken);
            }

            // Explicitly mark both entities modified.
            // This also protects you if query tracking behavior is changed globally later.
            _context.users.Update(user);
            _context.farmers.Update(farmer);

            var affected =
                await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"Profile saved successfully. Database rows affected: {affected}.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveProfileImageAsync(
            IFormFile file,
            int farmerId,
            CancellationToken cancellationToken)
        {
            var webRoot =
                _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            var folder = Path.Combine(
                webRoot,
                "uploads",
                "farmers");

            Directory.CreateDirectory(folder);

            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            var fileName =
                $"farmer_{farmerId}_{Guid.NewGuid():N}{extension}";

            var fullPath =
                Path.Combine(folder, fileName);

            await using var stream =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew);

            await file.CopyToAsync(
                stream,
                cancellationToken);

            return $"/uploads/farmers/{fileName}";
        }

        private async Task<Farmer?> GetCurrentFarmerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
                return null;

            return await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f =>
                        f.UserId == userId &&
                        f.IsActive,
                    cancellationToken);
        }
    }
}
