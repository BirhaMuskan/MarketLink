using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerPickupSlotsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerPickupSlotsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PICKUP SLOT LIST
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => fm.FarmerId == farmer.FarmerId)
                .ToListAsync(cancellationToken);

            var farmerMarketIds = farmerMarkets
                .Select(fm => fm.FarmerMarketId)
                .ToList();

            var marketIds = farmerMarkets
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var farmerMarketMap = farmerMarkets
                .ToDictionary(fm => fm.FarmerMarketId);

            var slots = await _context.pickupslots
                .AsNoTracking()
                .Where(ps => farmerMarketIds.Contains(ps.FarmerMarketId))
                .OrderBy(ps => ps.PickupDate)
                .ThenBy(ps => ps.StartTime)
                .ToListAsync(cancellationToken);

            var rows = slots.Select(ps =>
            {
                farmerMarketMap.TryGetValue(
                    ps.FarmerMarketId,
                    out var fm);

                Market? market = null;

                if (fm != null)
                {
                    markets.TryGetValue(
                        fm.MarketId,
                        out market);
                }

                return new FarmerPickupSlotRowViewModel
                {
                    PickupSlotId = ps.PickupSlotId,
                    FarmerMarketId = ps.FarmerMarketId,
                    MarketName = market?.MarketName ?? "Market",
                    StallNumber = fm?.StallNumber,
                    PickupDate = ps.PickupDate,
                    StartTime = ps.StartTime,
                    EndTime = ps.EndTime,
                    MaximumOrders = ps.MaximumOrders,
                    BookedOrders = ps.BookedOrders,
                    IsAvailable = ps.IsAvailable
                };
            }).ToList();

            return View(new FarmerPickupSlotsPageViewModel
            {
                Slots = rows,
                TotalSlots = rows.Count,
                AvailableSlots = rows.Count(x =>
                    x.IsAvailable &&
                    !x.IsFull &&
                    x.PickupDate.Date >= DateTime.Today),
                FullSlots = rows.Count(x => x.IsFull),
                TotalCapacity = rows.Sum(x => x.MaximumOrders),
                TotalBooked = rows.Sum(x => x.BookedOrders)
            });
        }

        // =========================================================
        // CREATE SLOT
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Create(
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var model = new CreatePickupSlotViewModel();

            await PopulateMarketDropdownAsync(
                model,
                farmer.FarmerId,
                cancellationToken);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreatePickupSlotViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId ==
                            model.FarmerMarketId &&
                        fm.FarmerId ==
                            farmer.FarmerId &&
                        fm.IsActive,
                    cancellationToken);

            if (farmerMarket == null)
            {
                ModelState.AddModelError(
                    nameof(model.FarmerMarketId),
                    "Please select one of your active markets.");
            }

            if (model.PickupDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.PickupDate),
                    "Pickup date cannot be in the past.");
            }

            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "End time must be later than start time.");
            }

            if (farmerMarket != null)
            {
                await ValidateAgainstFarmerScheduleAsync(
                    farmerMarket,
                    model.PickupDate,
                    model.StartTime,
                    model.EndTime,
                    cancellationToken);
            }

            var duplicate = await _context.pickupslots
                .AsNoTracking()
                .AnyAsync(
                    ps =>
                        ps.FarmerMarketId ==
                            model.FarmerMarketId &&
                        ps.PickupDate.Date ==
                            model.PickupDate.Date &&
                        ps.StartTime ==
                            model.StartTime &&
                        ps.EndTime ==
                            model.EndTime,
                    cancellationToken);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "",
                    "A pickup slot with the same date and time already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateMarketDropdownAsync(
                    model,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }

            _context.pickupslots.Add(new PickupSlot
            {
                FarmerMarketId = model.FarmerMarketId,
                PickupDate = model.PickupDate.Date,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                MaximumOrders = model.MaximumOrders,
                BookedOrders = 0,
                IsAvailable = model.IsAvailable
            });

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Pickup slot created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT SLOT
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var slot = await GetOwnedSlotAsync(
                id,
                farmer.FarmerId,
                cancellationToken);

            if (slot == null)
            {
                return NotFound();
            }

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstAsync(
                    fm => fm.FarmerMarketId == slot.FarmerMarketId,
                    cancellationToken);

            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == farmerMarket.MarketId,
                    cancellationToken);

            return View(new EditPickupSlotViewModel
            {
                PickupSlotId = slot.PickupSlotId,
                MarketName = market?.MarketName ?? "Market",
                StallNumber = farmerMarket.StallNumber,
                PickupDate = slot.PickupDate,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                MaximumOrders = slot.MaximumOrders,
                BookedOrders = slot.BookedOrders,
                IsAvailable = slot.IsAvailable
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditPickupSlotViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var slot = await GetOwnedSlotAsync(
                model.PickupSlotId,
                farmer.FarmerId,
                cancellationToken);

            if (slot == null)
            {
                return NotFound();
            }

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstAsync(
                    fm => fm.FarmerMarketId == slot.FarmerMarketId,
                    cancellationToken);

            if (model.PickupDate.Date < DateTime.Today &&
                model.PickupDate.Date != slot.PickupDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.PickupDate),
                    "Pickup date cannot be changed to a past date.");
            }

            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "End time must be later than start time.");
            }

            if (model.MaximumOrders < slot.BookedOrders)
            {
                ModelState.AddModelError(
                    nameof(model.MaximumOrders),
                    $"Maximum orders cannot be less than the {slot.BookedOrders} already booked orders.");
            }

            await ValidateAgainstFarmerScheduleAsync(
                farmerMarket,
                model.PickupDate,
                model.StartTime,
                model.EndTime,
                cancellationToken);

            if (!ModelState.IsValid)
            {
                var market = await _context.markets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.MarketId == farmerMarket.MarketId,
                        cancellationToken);

                model.MarketName =
                    market?.MarketName ?? "Market";

                model.StallNumber =
                    farmerMarket.StallNumber;

                model.BookedOrders =
                    slot.BookedOrders;

                return View(model);
            }

            slot.PickupDate = model.PickupDate.Date;
            slot.StartTime = model.StartTime;
            slot.EndTime = model.EndTime;
            slot.MaximumOrders = model.MaximumOrders;

            slot.IsAvailable =
                model.IsAvailable &&
                slot.BookedOrders < model.MaximumOrders;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Pickup slot updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TOGGLE AVAILABILITY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var slot = await GetOwnedSlotAsync(
                id,
                farmer.FarmerId,
                cancellationToken);

            if (slot == null)
            {
                return NotFound();
            }

            if (!slot.IsAvailable &&
                slot.BookedOrders >= slot.MaximumOrders)
            {
                TempData["ErrorMessage"] =
                    "This slot is full. Increase its capacity before making it available.";

                return RedirectToAction(nameof(Index));
            }

            slot.IsAvailable = !slot.IsAvailable;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                slot.IsAvailable
                    ? "Pickup slot is now available."
                    : "Pickup slot is now unavailable.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE / CANCEL SLOT
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var slot = await GetOwnedSlotAsync(
                id,
                farmer.FarmerId,
                cancellationToken);

            if (slot == null)
            {
                return NotFound();
            }

            if (slot.BookedOrders > 0)
            {
                slot.IsAvailable = false;

                await _context.SaveChangesAsync(cancellationToken);

                TempData["ErrorMessage"] =
                    "This slot already has bookings, so it was closed instead of deleted.";

                return RedirectToAction(nameof(Index));
            }

            _context.pickupslots.Remove(slot);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Pickup slot deleted.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private async Task ValidateAgainstFarmerScheduleAsync(
            FarmerMarket farmerMarket,
            DateTime pickupDate,
            TimeSpan startTime,
            TimeSpan endTime,
            CancellationToken cancellationToken)
        {
            var dayName = pickupDate.DayOfWeek.ToString();

            var marketDay = await _context.marketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    md =>
                        md.MarketId == farmerMarket.MarketId &&
                        md.DayName == dayName &&
                        md.IsActive,
                    cancellationToken);

            if (marketDay == null)
            {
                ModelState.AddModelError(
                    nameof(CreatePickupSlotViewModel.PickupDate),
                    $"{dayName} is not an active operating day for this market.");

                return;
            }

            var farmerSchedule = await _context.farmermarketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fmd =>
                        fmd.FarmerMarketId ==
                            farmerMarket.FarmerMarketId &&
                        fmd.MarketDayId ==
                            marketDay.MarketDayId &&
                        fmd.IsActive,
                    cancellationToken);

            if (farmerSchedule == null)
            {
                ModelState.AddModelError(
                    nameof(CreatePickupSlotViewModel.PickupDate),
                    $"You have not enabled {dayName} in My Markets → Pickup Schedule.");

                return;
            }

            if (startTime < farmerSchedule.PickupStartTime ||
                endTime > farmerSchedule.PickupEndTime)
            {
                ModelState.AddModelError(
                    "",
                    $"Slot time must be within your {dayName} pickup window " +
                    $"({FormatTime(farmerSchedule.PickupStartTime)} - " +
                    $"{FormatTime(farmerSchedule.PickupEndTime)}).");
            }
        }

        private async Task<PickupSlot?> GetOwnedSlotAsync(
            int pickupSlotId,
            int farmerId,
            CancellationToken cancellationToken)
        {
            return await _context.pickupslots
                .FirstOrDefaultAsync(
                    ps =>
                        ps.PickupSlotId == pickupSlotId &&
                        ps.FarmerMarket != null &&
                        ps.FarmerMarket.FarmerId == farmerId,
                    cancellationToken);
        }

        private async Task PopulateMarketDropdownAsync(
            CreatePickupSlotViewModel model,
            int farmerId,
            CancellationToken cancellationToken)
        {
            model.Markets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm =>
                    fm.FarmerId == farmerId &&
                    fm.IsActive &&
                    fm.Market != null &&
                    fm.Market.IsActive)
                .OrderBy(fm => fm.Market!.MarketName)
                .Select(fm => new SelectListItem
                {
                    Value = fm.FarmerMarketId.ToString(),

                    Text =
                        fm.Market!.MarketName +
                        (fm.StallNumber == null
                            ? ""
                            : " — Stall " + fm.StallNumber)
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<Farmer?> GetCurrentFarmerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f =>
                        f.UserId == userId &&
                        f.IsApproved &&
                        f.IsActive,
                    cancellationToken);
        }

        private static string FormatTime(TimeSpan time)
        {
            return DateTime.Today
                .Add(time)
                .ToString("hh:mm tt");
        }
    }
}
