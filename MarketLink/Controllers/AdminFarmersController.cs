using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFarmersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminFarmersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PENDING FARMER REQUESTS
        // GET: /AdminFarmers/Pending
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            var model = new FarmerApprovalPageViewModel
            {
                PendingFarmers = await _context.farmers
                    .AsNoTracking()
                    .Where(f => !f.IsApproved)
                    .OrderBy(f => f.CreatedAt)
                    .Select(f => new FarmerApprovalRowViewModel
                    {
                        FarmerId = f.FarmerId,
                        UserId = f.UserId,

                        FullName = f.User != null
                            ? f.User.FullName
                            : "",

                        BusinessName = f.BusinessName,

                        Email = f.User != null
                            ? f.User.Email
                            : "",

                        Phone = f.User != null
                            ? f.User.Phone
                            : "",

                        Address = f.Address,
                        Description = f.Description,
                        CreatedAt = f.CreatedAt,
                        IsApproved = f.IsApproved,

                        UserIsActive = f.User != null &&
                                       f.User.IsActive
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        // =========================================================
        // APPROVE FARMER
        // Farmer.IsApproved = true
        // Farmer.IsActive   = true
        // User.IsActive     = true
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int farmerId)
        {
            var farmer = await _context.farmers
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FarmerId == farmerId);

            if (farmer == null)
            {
                TempData["ErrorMessage"] = "Farmer request was not found.";
                return RedirectToAction(nameof(Pending));
            }

            if (farmer.IsApproved)
            {
                TempData["InfoMessage"] =
                    $"{farmer.BusinessName} is already approved.";

                return RedirectToAction(nameof(Pending));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                farmer.IsApproved = true;
                farmer.IsActive = true;

                if (farmer.User != null)
                {
                    farmer.User.IsActive = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"{farmer.BusinessName} has been approved successfully.";
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Farmer approval failed. No changes were saved.";
            }

            return RedirectToAction(nameof(Pending));
        }

        // =========================================================
        // REJECT FARMER
        //
        // We keep the database records for audit/history instead
        // of deleting the User and Farmer records.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int farmerId)
        {
            var farmer = await _context.farmers
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FarmerId == farmerId);

            if (farmer == null)
            {
                TempData["ErrorMessage"] = "Farmer request was not found.";
                return RedirectToAction(nameof(Pending));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                farmer.IsApproved = false;
                farmer.IsActive = false;

                if (farmer.User != null)
                {
                    farmer.User.IsActive = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"{farmer.BusinessName} has been rejected.";
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Farmer rejection failed. No changes were saved.";
            }

            return RedirectToAction(nameof(Pending));
        }
    }
}
