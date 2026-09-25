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
    public class FarmerMarketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerMarketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // MY MARKETS
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

            var assignments = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => fm.FarmerId == farmer.FarmerId)
                .OrderByDescending(fm => fm.IsActive)
                .ThenBy(fm => fm.Market!.MarketName)
                .ToListAsync(cancellationToken);

            var farmerMarketIds = assignments
                .Select(x => x.FarmerMarketId)
                .ToList();

            var marketIds = assignments
                .Select(x => x.MarketId)
                .ToList();

            var marketDays = await _context.marketdays
                .AsNoTracking()
                .Where(md => marketIds.Contains(md.MarketId) && md.IsActive)
                .ToListAsync(cancellationToken);

            var farmerDays = await _context.farmermarketdays
                .AsNoTracking()
                .Where(fmd => farmerMarketIds.Contains(fmd.FarmerMarketId))
                .ToListAsync(cancellationToken);

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var rows = assignments.Select(fm =>
            {
                markets.TryGetValue(fm.MarketId, out var market);

                var adminDays = marketDays
                    .Where(md => md.MarketId == fm.MarketId)
                    .OrderBy(md => DayOrder(md.DayName))
                    .ToList();

                var schedules = farmerDays
                    .Where(fmd =>
                        fmd.FarmerMarketId == fm.FarmerMarketId &&
                        fmd.IsActive)
                    .ToList();

                var scheduledNames =
                    from schedule in schedules
                    join marketDay in adminDays
                        on schedule.MarketDayId equals marketDay.MarketDayId
                    orderby DayOrder(marketDay.DayName)
                    select marketDay.DayName;

                return new FarmerMarketRowViewModel
                {
                    FarmerMarketId = fm.FarmerMarketId,
                    MarketId = fm.MarketId,
                    MarketName = market?.MarketName ?? "Market",
                    Address = market?.Address ?? "",
                    StallNumber = fm.StallNumber,
                    PickupInstructions = fm.PickupInstructions,
                    Latitude = market?.Latitude,
                    Longitude = market?.Longitude,
                    IsActive = fm.IsActive,

                    OperatingDaysText =
                        adminDays.Count == 0
                            ? "No market days configured"
                            : string.Join(", ", adminDays.Select(x => x.DayName)),

                    FarmerScheduleText =
                        !scheduledNames.Any()
                            ? "Not configured"
                            : string.Join(", ", scheduledNames),

                    ActiveScheduleCount = schedules.Count
                };
            }).ToList();

            var model = new FarmerMarketsPageViewModel
            {
                Markets = rows,
                TotalMarkets = rows.Count,
                ActiveMarkets = rows.Count(x => x.IsActive),
                ScheduledDays = rows.Sum(x => x.ActiveScheduleCount)
            };

            return View(model);
        }

        // =========================================================
        // JOIN / SELECT MARKET
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

            var model = new CreateFarmerMarketViewModel();

            await PopulateMarketDropdownAsync(
                model,
                farmer.FarmerId,
                cancellationToken);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateFarmerMarketViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == model.MarketId && m.IsActive,
                    cancellationToken);

            if (market == null)
            {
                ModelState.AddModelError(
                    nameof(model.MarketId),
                    "Please select a valid active market.");
            }

            var existing = await _context.farmermarkets
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerId == farmer.FarmerId &&
                        fm.MarketId == model.MarketId,
                    cancellationToken);

            if (existing != null && existing.IsActive)
            {
                ModelState.AddModelError(
                    nameof(model.MarketId),
                    "You are already assigned to this market.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateMarketDropdownAsync(
                    model,
                    farmer.FarmerId,
                    cancellationToken);

                return View(model);
            }

            if (existing != null)
            {
                existing.StallNumber =
                    CleanNullable(model.StallNumber);

                existing.PickupInstructions =
                    CleanNullable(model.PickupInstructions);

                existing.IsActive = true;
            }
            else
            {
                _context.farmermarkets.Add(new FarmerMarket
                {
                    FarmerId = farmer.FarmerId,
                    MarketId = model.MarketId,
                    StallNumber = CleanNullable(model.StallNumber),
                    PickupInstructions =
                        CleanNullable(model.PickupInstructions),
                    IsActive = true,
                    JoinedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Market added to your profile. Configure your pickup schedule next.";

            var assignment = await _context.farmermarkets
                .AsNoTracking()
                .FirstAsync(
                    fm =>
                        fm.FarmerId == farmer.FarmerId &&
                        fm.MarketId == model.MarketId,
                    cancellationToken);

            return RedirectToAction(
                nameof(Schedule),
                new { id = assignment.FarmerMarketId });
        }

        // =========================================================
        // EDIT FARMER'S STALL / PICKUP DETAILS
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

            var assignment = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == id &&
                        fm.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (assignment == null)
            {
                return NotFound();
            }

            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == assignment.MarketId,
                    cancellationToken);

            return View(new EditFarmerMarketViewModel
            {
                FarmerMarketId = assignment.FarmerMarketId,
                MarketName = market?.MarketName ?? "Market",
                Address = market?.Address ?? "",
                StallNumber = assignment.StallNumber,
                PickupInstructions = assignment.PickupInstructions,
                IsActive = assignment.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditFarmerMarketViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var assignment = await _context.farmermarkets
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == model.FarmerMarketId &&
                        fm.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (assignment == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var market = await _context.markets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.MarketId == assignment.MarketId,
                        cancellationToken);

                model.MarketName = market?.MarketName ?? "Market";
                model.Address = market?.Address ?? "";

                return View(model);
            }

            assignment.StallNumber =
                CleanNullable(model.StallNumber);

            assignment.PickupInstructions =
                CleanNullable(model.PickupInstructions);

            assignment.IsActive = model.IsActive;

            if (!assignment.IsActive)
            {
                var schedules = await _context.farmermarketdays
                    .Where(fmd =>
                        fmd.FarmerMarketId ==
                        assignment.FarmerMarketId)
                    .ToListAsync(cancellationToken);

                foreach (var schedule in schedules)
                {
                    schedule.IsActive = false;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Market details updated.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // PICKUP / ATTENDANCE SCHEDULE
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Schedule(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var assignment = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == id &&
                        fm.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (assignment == null)
            {
                return NotFound();
            }

            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == assignment.MarketId,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            var marketDays = await _context.marketdays
                .AsNoTracking()
                .Where(md =>
                    md.MarketId == market.MarketId &&
                    md.IsActive)
                .ToListAsync(cancellationToken);

            var farmerDays = await _context.farmermarketdays
                .AsNoTracking()
                .Where(fmd => fmd.FarmerMarketId == id)
                .ToListAsync(cancellationToken);

            var rows = marketDays
                .OrderBy(md => DayOrder(md.DayName))
                .Select(md =>
                {
                    var configured = farmerDays
                        .FirstOrDefault(fmd =>
                            fmd.MarketDayId == md.MarketDayId);

                    return new FarmerMarketScheduleRowViewModel
                    {
                        MarketDayId = md.MarketDayId,
                        FarmerMarketDayId =
                            configured?.FarmerMarketDayId,

                        DayName = md.DayName,

                        MarketOpeningTime = md.OpeningTime,
                        MarketClosingTime = md.ClosingTime,

                        PickupStartTime =
                            configured?.PickupStartTime
                            ?? md.OpeningTime,

                        PickupEndTime =
                            configured?.PickupEndTime
                            ?? md.ClosingTime,

                        OrderCutoffTime =
                            configured?.OrderCutoffTime
                            ?? md.OpeningTime,

                        IsConfigured = configured != null,
                        IsActive = configured?.IsActive ?? true
                    };
                })
                .ToList();

            return View(new FarmerMarketSchedulePageViewModel
            {
                FarmerMarketId = assignment.FarmerMarketId,
                MarketName = market.MarketName,
                Address = market.Address,
                Days = rows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDaySchedule(
            SaveFarmerMarketDayViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var assignment = await _context.farmermarkets
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == model.FarmerMarketId &&
                        fm.FarmerId == farmer.FarmerId &&
                        fm.IsActive,
                    cancellationToken);

            if (assignment == null)
            {
                return NotFound();
            }

            var marketDay = await _context.marketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    md =>
                        md.MarketDayId == model.MarketDayId &&
                        md.MarketId == assignment.MarketId &&
                        md.IsActive,
                    cancellationToken);

            if (marketDay == null)
            {
                TempData["ErrorMessage"] =
                    "The selected market day is no longer available.";

                return RedirectToAction(
                    nameof(Schedule),
                    new { id = model.FarmerMarketId });
            }

            if (model.PickupStartTime < marketDay.OpeningTime ||
                model.PickupStartTime > marketDay.ClosingTime)
            {
                ModelState.AddModelError(
                    nameof(model.PickupStartTime),
                    "Pickup start must be within market operating hours.");
            }

            if (model.PickupEndTime <= model.PickupStartTime ||
                model.PickupEndTime > marketDay.ClosingTime)
            {
                ModelState.AddModelError(
                    nameof(model.PickupEndTime),
                    "Pickup end must be after pickup start and within market hours.");
            }

            if (model.OrderCutoffTime > model.PickupStartTime)
            {
                ModelState.AddModelError(
                    nameof(model.OrderCutoffTime),
                    "Order cutoff cannot be later than pickup start.");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    string.Join(
                        " ",
                        ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage));

                return RedirectToAction(
                    nameof(Schedule),
                    new { id = model.FarmerMarketId });
            }

            var schedule = await _context.farmermarketdays
                .FirstOrDefaultAsync(
                    fmd =>
                        fmd.FarmerMarketId == model.FarmerMarketId &&
                        fmd.MarketDayId == model.MarketDayId,
                    cancellationToken);

            if (schedule == null)
            {
                schedule = new FarmerMarketDay
                {
                    FarmerMarketId = model.FarmerMarketId,
                    MarketDayId = model.MarketDayId
                };

                _context.farmermarketdays.Add(schedule);
            }

            schedule.PickupStartTime = model.PickupStartTime;
            schedule.PickupEndTime = model.PickupEndTime;
            schedule.OrderCutoffTime = model.OrderCutoffTime;
            schedule.IsActive = model.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"{marketDay.DayName} schedule saved.";

            return RedirectToAction(
                nameof(Schedule),
                new { id = model.FarmerMarketId });
        }

        // =========================================================
        // REMOVE FROM ACTIVE MARKETS
        // Soft deactivate to protect history.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var assignment = await _context.farmermarkets
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == id &&
                        fm.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (assignment == null)
            {
                return NotFound();
            }

            assignment.IsActive = false;

            var schedules = await _context.farmermarketdays
                .Where(fmd => fmd.FarmerMarketId == id)
                .ToListAsync(cancellationToken);

            foreach (var schedule in schedules)
            {
                schedule.IsActive = false;
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Market removed from your active markets.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // MAP
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Map(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var assignment = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == id &&
                        fm.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (assignment == null)
            {
                return NotFound();
            }

            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == assignment.MarketId,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            ViewBag.StallNumber = assignment.StallNumber;
            ViewBag.PickupInstructions = assignment.PickupInstructions;

            return View(market);
        }

        // =========================================================
        // HELPERS
        // =========================================================
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

        private async Task PopulateMarketDropdownAsync(
            CreateFarmerMarketViewModel model,
            int farmerId,
            CancellationToken cancellationToken)
        {
            var alreadyAssignedMarketIds =
                await _context.farmermarkets
                    .AsNoTracking()
                    .Where(fm =>
                        fm.FarmerId == farmerId &&
                        fm.IsActive)
                    .Select(fm => fm.MarketId)
                    .ToListAsync(cancellationToken);

            model.Markets = await _context.markets
                .AsNoTracking()
                .Where(m =>
                    m.IsActive &&
                    !alreadyAssignedMarketIds.Contains(m.MarketId))
                .OrderBy(m => m.MarketName)
                .Select(m => new SelectListItem
                {
                    Value = m.MarketId.ToString(),
                    Text = m.MarketName + " — " + m.Address
                })
                .ToListAsync(cancellationToken);
        }

        private static string? CleanNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static int DayOrder(string dayName)
        {
            return dayName switch
            {
                "Monday" => 1,
                "Tuesday" => 2,
                "Wednesday" => 3,
                "Thursday" => 4,
                "Friday" => 5,
                "Saturday" => 6,
                "Sunday" => 7,
                _ => 99
            };
        }
    }
}
