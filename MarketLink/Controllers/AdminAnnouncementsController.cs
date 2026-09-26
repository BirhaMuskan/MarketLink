using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminAnnouncementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? audience,
            CancellationToken cancellationToken)
        {
            var announcements = await _context.announcements
                .AsNoTracking()
                .OrderByDescending(a => a.PublishFrom)
                .ToListAsync(cancellationToken);

            var userIds = announcements
                .Select(a => a.CreatedByUserId)
                .Distinct()
                .ToList();

            var users = await _context.users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(
                    u => u.UserId,
                    cancellationToken);

            var now = DateTime.Now;

            var rows = announcements.Select(a =>
            {
                users.TryGetValue(a.CreatedByUserId, out var creator);

                string status;

                if (!a.IsActive)
                    status = "Inactive";
                else if (a.PublishFrom > now)
                    status = "Scheduled";
                else if (a.PublishUntil.HasValue &&
                         a.PublishUntil.Value < now)
                    status = "Expired";
                else
                    status = "Live";

                return new AdminAnnouncementRowViewModel
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Message = a.Message,
                    Audience = a.Audience,
                    CreatedByName = creator?.FullName ?? "Admin",
                    PublishFrom = a.PublishFrom,
                    PublishUntil = a.PublishUntil,
                    IsActive = a.IsActive,
                    Status = status
                };
            }).ToList();

            var total = rows.Count;
            var active = rows.Count(x => x.Status == "Live");
            var scheduled = rows.Count(x => x.Status == "Scheduled");
            var expired = rows.Count(x => x.Status == "Expired");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();

                rows = rows
                    .Where(x =>
                        x.Title.ToLower().Contains(term) ||
                        x.Message.ToLower().Contains(term))
                    .ToList();
            }

            var currentAudience =
                string.IsNullOrWhiteSpace(audience)
                    ? "ALL"
                    : audience.Trim().ToUpperInvariant();

            if (currentAudience != "ALL")
            {
                rows = rows
                    .Where(x => x.Audience == currentAudience)
                    .ToList();
            }

            return View(new AdminAnnouncementsPageViewModel
            {
                Announcements = rows,
                Total = total,
                Active = active,
                Scheduled = scheduled,
                Expired = expired,
                Search = search ?? "",
                Audience = currentAudience
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new AdminAnnouncementEditViewModel
            {
                PublishFrom = DateTime.Now,
                Audience = "ALL",
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminAnnouncementEditViewModel model,
            CancellationToken cancellationToken)
        {
            ValidateModel(model);

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return Forbid();

            var announcement = new Announcement
            {
                CreatedByUserId = userId.Value,
                Title = model.Title.Trim(),
                Message = model.Message.Trim(),
                Audience = model.Audience.Trim().ToUpperInvariant(),
                PublishFrom = model.PublishFrom,
                PublishUntil = model.PublishUntil,
                IsActive = model.IsActive
            };

            _context.announcements.Add(announcement);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Announcement created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            var announcement = await _context.announcements
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.AnnouncementId == id,
                    cancellationToken);

            if (announcement == null)
                return NotFound();

            return View(new AdminAnnouncementEditViewModel
            {
                AnnouncementId = announcement.AnnouncementId,
                Title = announcement.Title,
                Message = announcement.Message,
                Audience = announcement.Audience,
                PublishFrom = announcement.PublishFrom,
                PublishUntil = announcement.PublishUntil,
                IsActive = announcement.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AdminAnnouncementEditViewModel model,
            CancellationToken cancellationToken)
        {
            ValidateModel(model);

            if (!ModelState.IsValid)
                return View(model);

            var announcement = await _context.announcements
                .FirstOrDefaultAsync(
                    a => a.AnnouncementId == model.AnnouncementId,
                    cancellationToken);

            if (announcement == null)
                return NotFound();

            announcement.Title = model.Title.Trim();
            announcement.Message = model.Message.Trim();
            announcement.Audience = model.Audience.Trim().ToUpperInvariant();
            announcement.PublishFrom = model.PublishFrom;
            announcement.PublishUntil = model.PublishUntil;
            announcement.IsActive = model.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Announcement updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(
            int id,
            CancellationToken cancellationToken)
        {
            var announcement = await _context.announcements
                .FirstOrDefaultAsync(
                    a => a.AnnouncementId == id,
                    cancellationToken);

            if (announcement == null)
                return NotFound();

            announcement.IsActive = !announcement.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                announcement.IsActive
                    ? "Announcement activated."
                    : "Announcement deactivated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var announcement = await _context.announcements
                .FirstOrDefaultAsync(
                    a => a.AnnouncementId == id,
                    cancellationToken);

            if (announcement == null)
                return NotFound();

            _context.announcements.Remove(announcement);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Announcement deleted.";

            return RedirectToAction(nameof(Index));
        }

        private void ValidateModel(
            AdminAnnouncementEditViewModel model)
        {
            var allowedAudiences =
                new[] { "ALL", "CUSTOMER", "FARMER" };

            model.Audience =
                (model.Audience ?? "")
                .Trim()
                .ToUpperInvariant();

            if (!allowedAudiences.Contains(model.Audience))
            {
                ModelState.AddModelError(
                    nameof(model.Audience),
                    "Audience must be ALL, CUSTOMER or FARMER.");
            }

            if (model.PublishUntil.HasValue &&
                model.PublishUntil.Value < model.PublishFrom)
            {
                ModelState.AddModelError(
                    nameof(model.PublishUntil),
                    "Publish Until must be after Publish From.");
            }
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
