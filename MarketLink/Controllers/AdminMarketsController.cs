using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMarketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly string[] ValidDays =
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        public AdminMarketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var markets = await _context.markets
                .AsNoTracking()
                .OrderBy(m => m.MarketName)
                .ToListAsync(cancellationToken);

            var marketIds = markets
                .Select(m => m.MarketId)
                .ToList();

            var dayRows = await _context.marketdays
                .AsNoTracking()
                .Where(md => marketIds.Contains(md.MarketId))
                .ToListAsync(cancellationToken);

            var farmerCounts = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm =>
                    marketIds.Contains(fm.MarketId) &&
                    fm.IsActive)
                .GroupBy(fm => fm.MarketId)
                .Select(g => new
                {
                    MarketId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.MarketId,
                    x => x.Count,
                    cancellationToken);

            var rows = markets
                .Select(m =>
                {
                    var activeDays = dayRows
                        .Where(md =>
                            md.MarketId == m.MarketId &&
                            md.IsActive)
                        .OrderBy(md => DayOrder(md.DayName))
                        .ToList();

                    return new AdminMarketRowViewModel
                    {
                        MarketId = m.MarketId,
                        MarketName = m.MarketName,
                        Address = m.Address,
                        Description = m.Description,
                        Latitude = m.Latitude,
                        Longitude = m.Longitude,
                        MapProvider = m.MapProvider,
                        IsActive = m.IsActive,
                        OperatingDayCount = activeDays.Count,

                        FarmerCount =
                            farmerCounts.TryGetValue(
                                m.MarketId,
                                out var count)
                                ? count
                                : 0,

                        OperatingDaysText =
                            activeDays.Count == 0
                                ? "No operating days"
                                : string.Join(
                                    ", ",
                                    activeDays.Select(d => d.DayName))
                    };
                })
                .ToList();

            return View(new AdminMarketsPageViewModel
            {
                Markets = rows,
                TotalMarkets = rows.Count,
                ActiveMarkets = rows.Count(x => x.IsActive),
                InactiveMarkets = rows.Count(x => !x.IsActive)
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new AdminMarketFormViewModel
            {
                IsActive = true,
                MapProvider = "OpenStreetMap"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminMarketFormViewModel model,
            CancellationToken cancellationToken)
        {
            NormalizeMarket(model);

            if (!model.Latitude.HasValue ||
                !model.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Latitude),
                    "Please choose the market location on the map.");
            }

            var duplicateExists = await _context.markets
                .AsNoTracking()
                .AnyAsync(
                    m => m.MarketName.ToLower() ==
                         model.MarketName.ToLower(),
                    cancellationToken);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.MarketName),
                    "A market with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var market = new Market
            {
                MarketName = model.MarketName,
                Description = model.Description,
                Address = model.Address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                MapProvider = "OpenStreetMap",
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.markets.Add(market);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Market created successfully. Add its operating days next.";

            return RedirectToAction(
                nameof(Days),
                new { id = market.MarketId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == id,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            return View(new AdminMarketFormViewModel
            {
                MarketId = market.MarketId,
                MarketName = market.MarketName,
                Description = market.Description,
                Address = market.Address,
                Latitude = market.Latitude,
                Longitude = market.Longitude,
                MapProvider = "OpenStreetMap",
                IsActive = market.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AdminMarketFormViewModel model,
            CancellationToken cancellationToken)
        {
            NormalizeMarket(model);

            var market = await _context.markets
                .FirstOrDefaultAsync(
                    m => m.MarketId == model.MarketId,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            if (!model.Latitude.HasValue ||
                !model.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Latitude),
                    "Please choose the market location on the map.");
            }

            var duplicateExists = await _context.markets
                .AsNoTracking()
                .AnyAsync(
                    m =>
                        m.MarketId != model.MarketId &&
                        m.MarketName.ToLower() ==
                        model.MarketName.ToLower(),
                    cancellationToken);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.MarketName),
                    "Another market with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            market.MarketName = model.MarketName;
            market.Description = model.Description;
            market.Address = model.Address;
            market.Latitude = model.Latitude;
            market.Longitude = model.Longitude;
            market.MapProvider = "OpenStreetMap";
            market.IsActive = model.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Market updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == id,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            return View(market);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id,
            CancellationToken cancellationToken)
        {
            var market = await _context.markets
                .FirstOrDefaultAsync(
                    m => m.MarketId == id,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            market.IsActive = !market.IsActive;

            if (!market.IsActive)
            {
                var farmerMarkets = await _context.farmermarkets
                    .Where(fm =>
                        fm.MarketId == id &&
                        fm.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var farmerMarket in farmerMarkets)
                {
                    farmerMarket.IsActive = false;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                market.IsActive
                    ? "Market activated."
                    : "Market deactivated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Days(
            int id,
            CancellationToken cancellationToken)
        {
            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == id,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            var days = await _context.marketdays
                .AsNoTracking()
                .Where(md => md.MarketId == id)
                .ToListAsync(cancellationToken);

            return View(new AdminMarketDaysPageViewModel
            {
                MarketId = market.MarketId,
                MarketName = market.MarketName,

                Days = days
                    .OrderBy(d => DayOrder(d.DayName))
                    .Select(d => new AdminMarketDayRowViewModel
                    {
                        MarketDayId = d.MarketDayId,
                        DayName = d.DayName,
                        OpeningTime = d.OpeningTime,
                        ClosingTime = d.ClosingTime,
                        IsActive = d.IsActive
                    })
                    .ToList(),

                NewDay = new AdminMarketDayFormViewModel
                {
                    MarketId = market.MarketId,
                    IsActive = true,
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(14, 0, 0)
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDay(
            AdminMarketDayFormViewModel model,
            CancellationToken cancellationToken)
        {
            var marketExists = await _context.markets
                .AsNoTracking()
                .AnyAsync(
                    m => m.MarketId == model.MarketId,
                    cancellationToken);

            if (!marketExists)
            {
                return NotFound();
            }

            NormalizeDay(model);
            ValidateDay(model);

            var duplicate = await _context.marketdays
                .AsNoTracking()
                .AnyAsync(
                    md =>
                        md.MarketId == model.MarketId &&
                        md.DayName == model.DayName,
                    cancellationToken);

            if (duplicate)
            {
                TempData["ErrorMessage"] =
                    $"{model.DayName} already exists for this market.";

                return RedirectToAction(
                    nameof(Days),
                    new { id = model.MarketId });
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
                    nameof(Days),
                    new { id = model.MarketId });
            }

            _context.marketdays.Add(new MarketDay
            {
                MarketId = model.MarketId,
                DayName = model.DayName,
                OpeningTime = model.OpeningTime,
                ClosingTime = model.ClosingTime,
                IsActive = model.IsActive
            });

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"{model.DayName} operating hours added.";

            return RedirectToAction(
                nameof(Days),
                new { id = model.MarketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDay(
            int marketDayId,
            CancellationToken cancellationToken)
        {
            var day = await _context.marketdays
                .FirstOrDefaultAsync(
                    md => md.MarketDayId == marketDayId,
                    cancellationToken);

            if (day == null)
            {
                return NotFound();
            }

            day.IsActive = !day.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                day.IsActive
                    ? $"{day.DayName} activated."
                    : $"{day.DayName} deactivated.";

            return RedirectToAction(
                nameof(Days),
                new { id = day.MarketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDay(
            int marketDayId,
            CancellationToken cancellationToken)
        {
            var day = await _context.marketdays
                .FirstOrDefaultAsync(
                    md => md.MarketDayId == marketDayId,
                    cancellationToken);

            if (day == null)
            {
                return NotFound();
            }

            var usedByFarmerSchedule =
                await _context.farmermarketdays
                    .AsNoTracking()
                    .AnyAsync(
                        fmd => fmd.MarketDayId == marketDayId,
                        cancellationToken);

            if (usedByFarmerSchedule)
            {
                day.IsActive = false;

                await _context.SaveChangesAsync(cancellationToken);

                TempData["ErrorMessage"] =
                    "This day is already used by a farmer schedule, so it was deactivated instead of deleted.";

                return RedirectToAction(
                    nameof(Days),
                    new { id = day.MarketId });
            }

            var marketId = day.MarketId;

            _context.marketdays.Remove(day);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Operating day removed.";

            return RedirectToAction(
                nameof(Days),
                new { id = marketId });
        }

        private void NormalizeMarket(
            AdminMarketFormViewModel model)
        {
            model.MarketName =
                model.MarketName?.Trim() ?? "";

            model.Description =
                model.Description?.Trim() ?? "";

            model.Address =
                model.Address?.Trim() ?? "";

            model.MapProvider = "OpenStreetMap";
        }

        private void NormalizeDay(
            AdminMarketDayFormViewModel model)
        {
            model.DayName =
                model.DayName?.Trim() ?? "";
        }

        private void ValidateDay(
            AdminMarketDayFormViewModel model)
        {
            if (!ValidDays.Contains(
                model.DayName,
                StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.DayName),
                    "Please select a valid day.");
            }
            else
            {
                model.DayName =
                    ValidDays.First(d =>
                        d.Equals(
                            model.DayName,
                            StringComparison.OrdinalIgnoreCase));
            }

            if (model.ClosingTime <= model.OpeningTime)
            {
                ModelState.AddModelError(
                    nameof(model.ClosingTime),
                    "Closing time must be later than opening time.");
            }
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
