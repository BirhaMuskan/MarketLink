using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerMarketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerMarketsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
            {
                return Forbid();
            }

            var marketQuery = _context.markets
                .AsNoTracking()
                .Where(m => m.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                marketQuery = marketQuery.Where(m =>
                    m.MarketName.Contains(term) ||
                    m.Address.Contains(term));
            }

            var markets = await marketQuery
                .OrderBy(m => m.MarketName)
                .ToListAsync(cancellationToken);

            var marketIds = markets
                .Select(m => m.MarketId)
                .ToList();

            var marketDays = await _context.marketdays
                .AsNoTracking()
                .Where(md =>
                    marketIds.Contains(md.MarketId) &&
                    md.IsActive)
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
                    Count = g.Select(x => x.FarmerId).Distinct().Count()
                })
                .ToDictionaryAsync(
                    x => x.MarketId,
                    x => x.Count,
                    cancellationToken);

            var favorites = await _context.favoritemarkets
                .AsNoTracking()
                .Where(fm => fm.CustomerId == customer.CustomerId)
                .Select(fm => fm.MarketId)
                .ToListAsync(cancellationToken);

            var rows = markets.Select(m =>
            {
                var days = marketDays
                    .Where(md => md.MarketId == m.MarketId)
                    .OrderBy(md => DayOrder(md.DayName))
                    .Select(md => md.DayName)
                    .ToList();

                return new CustomerMarketCardViewModel
                {
                    MarketId = m.MarketId,
                    MarketName = m.MarketName,
                    Description = m.Description,
                    Address = m.Address,
                    Latitude = m.Latitude,
                    Longitude = m.Longitude,
                    OperatingDaysText =
                        days.Count == 0
                            ? "Schedule not available"
                            : string.Join(", ", days),
                    FarmerCount =
                        farmerCounts.TryGetValue(
                            m.MarketId,
                            out var count)
                            ? count
                            : 0,
                    IsFavorite =
                        favorites.Contains(m.MarketId)
                };
            }).ToList();

            return View(new CustomerMarketsPageViewModel
            {
                Markets = rows,
                TotalMarkets = rows.Count,
                SavedMarkets = favorites.Count
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
            {
                return Forbid();
            }

            var market = await _context.markets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.MarketId == id && m.IsActive,
                    cancellationToken);

            if (market == null)
            {
                return NotFound();
            }

            var days = await _context.marketdays
                .AsNoTracking()
                .Where(md =>
                    md.MarketId == id &&
                    md.IsActive)
                .OrderBy(md => md.DayName)
                .ToListAsync(cancellationToken);

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm =>
                    fm.MarketId == id &&
                    fm.IsActive)
                .ToListAsync(cancellationToken);

            var farmerIds = farmerMarkets
                .Select(fm => fm.FarmerId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f =>
                    farmerIds.Contains(f.FarmerId) &&
                    f.IsApproved &&
                    f.IsActive)
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var farmerRows = farmerMarkets
                .Where(fm => farmers.ContainsKey(fm.FarmerId))
                .Select(fm =>
                {
                    var farmer = farmers[fm.FarmerId];

                    return new CustomerMarketFarmerViewModel
                    {
                        FarmerId = farmer.FarmerId,
                        BusinessName = farmer.BusinessName,
                        StallNumber = fm.StallNumber,
                        ProfileImageUrl = farmer.ProfileImageUrl,
                        Description = farmer.Description
                    };
                })
                .OrderBy(x => x.BusinessName)
                .ToList();

            var isFavorite = await _context.favoritemarkets
                .AsNoTracking()
                .AnyAsync(
                    fm =>
                        fm.CustomerId == customer.CustomerId &&
                        fm.MarketId == id,
                    cancellationToken);

            return View(new CustomerMarketDetailsViewModel
            {
                MarketId = market.MarketId,
                MarketName = market.MarketName,
                Description = market.Description,
                Address = market.Address,
                Latitude = market.Latitude,
                Longitude = market.Longitude,
                IsFavorite = isFavorite,

                OperatingDays = days
                    .OrderBy(d => DayOrder(d.DayName))
                    .Select(d => new CustomerMarketDayViewModel
                    {
                        DayName = d.DayName,
                        OpeningTime = d.OpeningTime,
                        ClosingTime = d.ClosingTime
                    })
                    .ToList(),

                Farmers = farmerRows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(
            int marketId,
            string? returnUrl,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
            {
                return Forbid();
            }

            var marketExists = await _context.markets
                .AsNoTracking()
                .AnyAsync(
                    m => m.MarketId == marketId && m.IsActive,
                    cancellationToken);

            if (!marketExists)
            {
                return NotFound();
            }

            var favorite = await _context.favoritemarkets
                .FirstOrDefaultAsync(
                    fm =>
                        fm.CustomerId == customer.CustomerId &&
                        fm.MarketId == marketId,
                    cancellationToken);

            if (favorite == null)
            {
                _context.favoritemarkets.Add(
                    new FavoriteMarket
                    {
                        CustomerId = customer.CustomerId,
                        MarketId = marketId,
                        CreatedAt = DateTime.Now
                    });
            }
            else
            {
                _context.favoritemarkets.Remove(favorite);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<Customer?> GetCurrentCustomerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId,
                    cancellationToken);
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
