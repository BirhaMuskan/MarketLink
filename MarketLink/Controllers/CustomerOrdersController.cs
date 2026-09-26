using System.Data;
using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? filter,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var currentFilter =
                string.IsNullOrWhiteSpace(filter)
                    ? "All"
                    : filter.Trim();

            var query = _context.orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.CustomerId);

            query = currentFilter switch
            {
                "Placed" =>
                    query.Where(o => o.OrderStatus == "Placed"),

                "Accepted" =>
                    query.Where(o => o.OrderStatus == "Accepted"),

                "Ready for Pickup" =>
                    query.Where(o => o.OrderStatus == "Ready for Pickup"),

                "Completed" =>
                    query.Where(o => o.OrderStatus == "Completed"),

                "Cancelled" =>
                    query.Where(o => o.OrderStatus == "Cancelled"),

                "Declined" =>
                    query.Where(o => o.OrderStatus == "Declined"),

                _ => query
            };

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var allOrders = await _context.orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.CustomerId)
                .ToListAsync(cancellationToken);

            var farmerIds = orders
                .Select(o => o.FarmerId)
                .Distinct()
                .ToList();

            var farmers = await _context.farmers
                .AsNoTracking()
                .Where(f => farmerIds.Contains(f.FarmerId))
                .ToDictionaryAsync(
                    f => f.FarmerId,
                    cancellationToken);

            var farmerMarketIds = orders
                .Select(o => o.FarmerMarketId)
                .Distinct()
                .ToList();

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => farmerMarketIds.Contains(fm.FarmerMarketId))
                .ToDictionaryAsync(
                    fm => fm.FarmerMarketId,
                    cancellationToken);

            var marketIds = farmerMarkets.Values
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(
                    m => m.MarketId,
                    cancellationToken);

            var orderIds = orders
                .Select(o => o.OrderId)
                .ToList();

            var itemCounts = await _context.orderitems
                .AsNoTracking()
                .Where(oi => orderIds.Contains(oi.OrderId))
                .GroupBy(oi => oi.OrderId)
                .Select(g => new
                {
                    OrderId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.OrderId,
                    x => x.Count,
                    cancellationToken);

            var rows = orders.Select(o =>
            {
                farmers.TryGetValue(o.FarmerId, out var farmer);
                farmerMarkets.TryGetValue(o.FarmerMarketId, out var fm);

                Market? market = null;

                if (fm != null)
                {
                    markets.TryGetValue(fm.MarketId, out market);
                }

                return new CustomerOrderRowViewModel
                {
                    OrderId = o.OrderId,
                    OrderNo = o.OrderNo,
                    FarmerName = farmer?.BusinessName ?? "Farmer",
                    MarketName = market?.MarketName ?? "Market",
                    OrderDate = o.OrderDate,
                    PickupDate = o.PickupDate,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    ItemCount =
                        itemCounts.TryGetValue(o.OrderId, out var count)
                            ? count
                            : 0
                };
            }).ToList();

            var activeStatuses =
                new[] { "Placed", "Accepted", "Ready for Pickup" };

            return View(new CustomerOrdersPageViewModel
            {
                CurrentFilter = currentFilter,
                TotalOrders = allOrders.Count,
                ActiveOrders =
                    allOrders.Count(o =>
                        activeStatuses.Contains(o.OrderStatus)),
                CompletedOrders =
                    allOrders.Count(o =>
                        o.OrderStatus == "Completed"),
                CancelledOrders =
                    allOrders.Count(o =>
                        o.OrderStatus == "Cancelled" ||
                        o.OrderStatus == "Declined"),
                Orders = rows
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var order = await _context.orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == id &&
                        o.CustomerId == customer.CustomerId,
                    cancellationToken);

            if (order == null)
                return NotFound();

            var items = await _context.orderitems
                .AsNoTracking()
                .Where(oi => oi.OrderId == order.OrderId)
                .OrderBy(oi => oi.OrderItemId)
                .ToListAsync(cancellationToken);

            var history = await _context.orderstatushistories
                .AsNoTracking()
                .Where(h => h.OrderId == order.OrderId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync(cancellationToken);

            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FarmerId == order.FarmerId,
                    cancellationToken);

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm => fm.FarmerMarketId == order.FarmerMarketId,
                    cancellationToken);

            Market? market = null;

            if (farmerMarket != null)
            {
                market = await _context.markets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.MarketId == farmerMarket.MarketId,
                        cancellationToken);
            }

            PickupSlot? slot = null;

            if (order.PickupSlotId.HasValue)
            {
                slot = await _context.pickupslots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        ps => ps.PickupSlotId == order.PickupSlotId.Value,
                        cancellationToken);
            }

            var cancellation =
                await GetCancellationEligibilityAsync(
                    order,
                    farmerMarket,
                    slot,
                    cancellationToken);

            return View(new CustomerOrderDetailsViewModel
            {
                OrderId = order.OrderId,
                OrderNo = order.OrderNo,
                FarmerName = farmer?.BusinessName ?? "Farmer",
                MarketName = market?.MarketName ?? "Market",
                MarketAddress = market?.Address ?? "",
                StallNumber = farmerMarket?.StallNumber,
                OrderDate = order.OrderDate,
                PickupDate = order.PickupDate,
                PickupTimeText =
                    slot == null
                        ? ""
                        : $"{DateTime.Today.Add(slot.StartTime):hh:mm tt} - " +
                          $"{DateTime.Today.Add(slot.EndTime):hh:mm tt}",
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                CustomerNotes = order.CustomerNotes,
                FarmerNotes = order.FarmerNotes,
                CanCancel = cancellation.CanCancel,
                CancellationMessage = cancellation.Message,

                Items = items.Select(i =>
                    new CustomerOrderItemViewModel
                    {
                        ProductName = i.ProductName,
                        UnitName = i.UnitName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice
                    }).ToList(),

                StatusHistory = history.Select(h =>
                    new CustomerOrderStatusHistoryViewModel
                    {
                        PreviousStatus = h.PreviousStatus,
                        NewStatus = h.NewStatus,
                        Remarks = h.Remarks,
                        ChangedAt = h.ChangedAt
                    }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                var order = await _context.orders
                    .FirstOrDefaultAsync(
                        o =>
                            o.OrderId == id &&
                            o.CustomerId == customer.CustomerId,
                        cancellationToken);

                if (order == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound();
                }

                var farmerMarket = await _context.farmermarkets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        fm => fm.FarmerMarketId == order.FarmerMarketId,
                        cancellationToken);

                PickupSlot? slot = null;

                if (order.PickupSlotId.HasValue)
                {
                    slot = await _context.pickupslots
                        .FirstOrDefaultAsync(
                            ps =>
                                ps.PickupSlotId ==
                                order.PickupSlotId.Value,
                            cancellationToken);
                }

                var eligibility =
                    await GetCancellationEligibilityAsync(
                        order,
                        farmerMarket,
                        slot,
                        cancellationToken);

                if (!eligibility.CanCancel)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["ErrorMessage"] =
                        eligibility.Message;

                    return RedirectToAction(
                        nameof(Details),
                        new { id = order.OrderId });
                }

                var orderItems = await _context.orderitems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .ToListAsync(cancellationToken);

                foreach (var item in orderItems)
                {
                    if (!item.InventoryId.HasValue)
                        continue;

                    var inventory = await _context.inventories
                        .FirstOrDefaultAsync(
                            i =>
                                i.InventoryId ==
                                item.InventoryId.Value,
                            cancellationToken);

                    if (inventory == null)
                        continue;

                    inventory.ReservedQuantity =
                        Math.Max(
                            0,
                            inventory.ReservedQuantity -
                            item.Quantity);

                    var available =
                        inventory.StockQuantity -
                        inventory.ReservedQuantity -
                        inventory.SoldQuantity;

                    if (available > 0)
                    {
                        inventory.IsSoldOut = false;
                        inventory.IsAvailable = true;
                    }
                }

                if (slot != null)
                {
                    slot.BookedOrders =
                        Math.Max(
                            0,
                            slot.BookedOrders - 1);

                    if (slot.BookedOrders < slot.MaximumOrders)
                    {
                        slot.IsAvailable = true;
                    }
                }

                var previousStatus =
                    order.OrderStatus;

                order.OrderStatus = "Cancelled";

                _context.orderstatushistories.Add(
                    new OrderStatusHistory
                    {
                        OrderId = order.OrderId,
                        PreviousStatus = previousStatus,
                        NewStatus = "Cancelled",
                        ChangedByUserId = customer.UserId,
                        Remarks = "Order cancelled by customer.",
                        ChangedAt = DateTime.Now
                    });

                var farmer = await _context.farmers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        f => f.FarmerId == order.FarmerId,
                        cancellationToken);

                if (farmer != null)
                {
                    _context.notifications.Add(
                        new Notification
                        {
                            UserId = farmer.UserId,
                            Title = "Order Cancelled",
                            Message =
                                $"Customer cancelled order {order.OrderNo}.",
                            NotificationType = "OrderUpdate",
                            ActionUrl =
                                $"/FarmerOrders/Details/{order.OrderId}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Your order has been cancelled and reserved stock has been released.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<(bool CanCancel, string Message)>
            GetCancellationEligibilityAsync(
                Order order,
                FarmerMarket? farmerMarket,
                PickupSlot? slot,
                CancellationToken cancellationToken)
        {
            if (order.OrderStatus != "Placed" &&
                order.OrderStatus != "Accepted")
            {
                return (
                    false,
                    "This order can no longer be cancelled.");
            }

            if (farmerMarket == null ||
                slot == null)
            {
                return (
                    false,
                    "Pickup schedule information is unavailable.");
            }

            var marketDay = await _context.marketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    md =>
                        md.MarketId == farmerMarket.MarketId &&
                        md.IsActive &&
                        md.DayName ==
                        slot.PickupDate.DayOfWeek.ToString(),
                    cancellationToken);

            if (marketDay == null)
            {
                return (
                    false,
                    "The pickup market schedule is unavailable.");
            }

            var farmerMarketDay = await _context.farmermarketdays
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fmd =>
                        fmd.FarmerMarketId ==
                        farmerMarket.FarmerMarketId &&
                        fmd.MarketDayId ==
                        marketDay.MarketDayId &&
                        fmd.IsActive,
                    cancellationToken);

            if (farmerMarketDay == null)
            {
                return (
                    false,
                    "The farmer pickup schedule is unavailable.");
            }

            var cutoff =
                slot.PickupDate.Date +
                farmerMarketDay.OrderCutoffTime;

            if (DateTime.Now > cutoff)
            {
                return (
                    false,
                    $"Cancellation cutoff passed at {cutoff:dd MMM yyyy hh:mm tt}.");
            }

            return (
                true,
                $"You can cancel until {cutoff:dd MMM yyyy hh:mm tt}.");
        }

        private async Task<Customer?> GetCurrentCustomerAsync(
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(userIdValue, out var userId))
                return null;

            return await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId,
                    cancellationToken);
        }
    }
}
