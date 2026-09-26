using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            CancellationToken cancellationToken)
        {
            var customers = await _context.customers
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var userIds = customers
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var users = await _context.users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(
                    u => u.UserId,
                    cancellationToken);

            var customerIds = customers
                .Select(c => c.CustomerId)
                .Distinct()
                .ToList();

            var orders = await _context.orders
                .AsNoTracking()
                .Where(o => customerIds.Contains(o.CustomerId))
                .ToListAsync(cancellationToken);

            var rows = customers
                .Where(c => users.ContainsKey(c.UserId))
                .Select(c =>
                {
                    var user = users[c.UserId];

                    var customerOrders = orders
                        .Where(o => o.CustomerId == c.CustomerId)
                        .ToList();

                    return new AdminCustomerRowViewModel
                    {
                        CustomerId = c.CustomerId,
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Address = user.Address,
                        IsActive = user.IsActive,
                        AccountCreatedAt = user.CreatedAt,
                        CustomerCreatedAt = c.CreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        TotalOrders = customerOrders.Count,
                        CompletedOrders = customerOrders.Count(o =>
                            o.OrderStatus == "Completed"),
                        CancelledOrders = customerOrders.Count(o =>
                            o.OrderStatus == "Cancelled"),
                        TotalSpent = customerOrders
                            .Where(o => o.OrderStatus == "Completed")
                            .Sum(o => o.TotalAmount)
                    };
                })
                .ToList();

            var totalCustomers = rows.Count;
            var activeCustomers = rows.Count(x => x.IsActive);
            var inactiveCustomers = rows.Count(x => !x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();

                rows = rows
                    .Where(x =>
                        x.FullName.ToLower().Contains(term) ||
                        x.Email.ToLower().Contains(term) ||
                        (x.Phone ?? "").ToLower().Contains(term))
                    .ToList();
            }

            var currentStatus =
                string.IsNullOrWhiteSpace(status)
                    ? "All"
                    : status.Trim();

            if (currentStatus == "Active")
                rows = rows.Where(x => x.IsActive).ToList();
            else if (currentStatus == "Inactive")
                rows = rows.Where(x => !x.IsActive).ToList();

            rows = rows
                .OrderByDescending(x => x.CustomerCreatedAt)
                .ToList();

            return View(new AdminCustomersPageViewModel
            {
                Customers = rows,
                TotalCustomers = totalCustomers,
                ActiveCustomers = activeCustomers,
                InactiveCustomers = inactiveCustomers,
                Search = search ?? "",
                Status = currentStatus
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CustomerId == id,
                    cancellationToken);

            if (customer == null)
                return NotFound();

            var user = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.UserId == customer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            var orders = await _context.orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var farmerIds = orders
                .Select(o => o.FarmerId)
                .Distinct()
                .ToList();

            var marketIds = orders
                .Select(o => o.FarmerMarketId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => marketIds.Contains(fm.FarmerMarketId))
                .ToDictionaryAsync(
                    fm => fm.FarmerMarketId,
                    cancellationToken);

            var actualMarketIds = farmerMarkets.Values
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => actualMarketIds.Contains(m.MarketId))
                .ToDictionaryAsync(
                    m => m.MarketId,
                    cancellationToken);

            var recentOrders = orders
                .Take(10)
                .Select(o =>
                {
                    farmers.TryGetValue(o.FarmerId, out var farmer);

                    string marketName = "Market";
                    if (farmerMarkets.TryGetValue(o.FarmerMarketId, out var fm) &&
                        markets.TryGetValue(fm.MarketId, out var market))
                    {
                        marketName = market.MarketName;
                    }

                    return new AdminCustomerOrderRowViewModel
                    {
                        OrderId = o.OrderId,
                        OrderNo = o.OrderNo,
                        FarmerName = farmer?.BusinessName ?? "Farmer",
                        MarketName = marketName,
                        OrderStatus = o.OrderStatus,
                        TotalAmount = o.TotalAmount,
                        OrderDate = o.OrderDate,
                        PickupDate = o.PickupDate
                    };
                })
                .ToList();

            return View(new AdminCustomerDetailsViewModel
            {
                CustomerId = customer.CustomerId,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                IsActive = user.IsActive,
                AccountCreatedAt = user.CreatedAt,
                CustomerCreatedAt = customer.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o =>
                    o.OrderStatus == "Completed"),
                CancelledOrders = orders.Count(o =>
                    o.OrderStatus == "Cancelled"),
                TotalSpent = orders
                    .Where(o => o.OrderStatus == "Completed")
                    .Sum(o => o.TotalAmount),
                RecentOrders = recentOrders
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CustomerId == id,
                    cancellationToken);

            if (customer == null)
                return NotFound();

            var user = await _context.users
                .FirstOrDefaultAsync(
                    u => u.UserId == customer.UserId,
                    cancellationToken);

            if (user == null)
                return NotFound();

            user.IsActive = !user.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                user.IsActive
                    ? "Customer account activated."
                    : "Customer account deactivated.";

            return RedirectToAction(nameof(Index));
        }
    }
}
