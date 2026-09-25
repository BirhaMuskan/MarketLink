using System.Security.Claims;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    [Authorize(Roles = "Farmer")]
    public class FarmerOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FarmerOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ORDER LIST
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? status,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var query = _context.orders
                .AsNoTracking()
                .Where(o => o.FarmerId == farmer.FarmerId);

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var orderIds = orders
                .Select(o => o.OrderId)
                .ToList();

            var customerIds = orders
                .Select(o => o.CustomerId)
                .Distinct()
                .ToList();

            var farmerMarketIds = orders
                .Select(o => o.FarmerMarketId)
                .Distinct()
                .ToList();

            var pickupSlotIds = orders
                .Where(o => o.PickupSlotId.HasValue)
                .Select(o => o.PickupSlotId!.Value)
                .Distinct()
                .ToList();

            var customers = await _context.customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync(cancellationToken);

            var userIds = customers
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            var users = await _context.users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, cancellationToken);

            var customerMap = customers
                .ToDictionary(c => c.CustomerId);

            var farmerMarkets = await _context.farmermarkets
                .AsNoTracking()
                .Where(fm => farmerMarketIds.Contains(fm.FarmerMarketId))
                .ToListAsync(cancellationToken);

            var farmerMarketMap = farmerMarkets
                .ToDictionary(fm => fm.FarmerMarketId);

            var marketIds = farmerMarkets
                .Select(fm => fm.MarketId)
                .Distinct()
                .ToList();

            var markets = await _context.markets
                .AsNoTracking()
                .Where(m => marketIds.Contains(m.MarketId))
                .ToDictionaryAsync(m => m.MarketId, cancellationToken);

            var pickupSlots = await _context.pickupslots
                .AsNoTracking()
                .Where(ps => pickupSlotIds.Contains(ps.PickupSlotId))
                .ToDictionaryAsync(ps => ps.PickupSlotId, cancellationToken);

            var orderItems = await _context.orderitems
                .AsNoTracking()
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync(cancellationToken);

            var rows = orders.Select(o =>
            {
                customerMap.TryGetValue(o.CustomerId, out var customer);

                User? customerUser = null;

                if (customer != null)
                {
                    users.TryGetValue(customer.UserId, out customerUser);
                }

                farmerMarketMap.TryGetValue(
                    o.FarmerMarketId,
                    out var farmerMarket);

                Market? market = null;

                if (farmerMarket != null)
                {
                    markets.TryGetValue(
                        farmerMarket.MarketId,
                        out market);
                }

                PickupSlot? pickupSlot = null;

                if (o.PickupSlotId.HasValue)
                {
                    pickupSlots.TryGetValue(
                        o.PickupSlotId.Value,
                        out pickupSlot);
                }

                var items = orderItems
                    .Where(oi => oi.OrderId == o.OrderId)
                    .ToList();

                return new FarmerOrderRowViewModel
                {
                    OrderId = o.OrderId,
                    OrderNo = o.OrderNo,

                    CustomerName =
                        customerUser?.FullName ?? "Customer",

                    CustomerEmail =
                        customerUser?.Email ?? "",

                    CustomerPhone =
                        customerUser?.Phone ?? "",

                    MarketName =
                        market?.MarketName ?? "Market",

                    StallNumber =
                        farmerMarket?.StallNumber,

                    PickupDate = o.PickupDate,

                    PickupStartTime =
                        pickupSlot?.StartTime,

                    PickupEndTime =
                        pickupSlot?.EndTime,

                    TotalAmount = o.TotalAmount,

                    OrderStatus = o.OrderStatus,

                    CustomerNotes = o.CustomerNotes,
                    FarmerNotes = o.FarmerNotes,

                    OrderDate = o.OrderDate,

                    ItemCount = items.Count,

                    TotalQuantity =
                        items.Sum(x => x.Quantity)
                };
            }).ToList();

            var allOrders = await _context.orders
                .AsNoTracking()
                .Where(o => o.FarmerId == farmer.FarmerId)
                .ToListAsync(cancellationToken);

            return View(new FarmerOrdersPageViewModel
            {
                Orders = rows,

                TotalOrders = allOrders.Count,

                PlacedOrders =
                    allOrders.Count(o => o.OrderStatus == "Placed"),

                AcceptedOrders =
                    allOrders.Count(o => o.OrderStatus == "Accepted"),

                ReadyOrders =
                    allOrders.Count(o => o.OrderStatus == "Ready for Pickup"),

                CompletedOrders =
                    allOrders.Count(o => o.OrderStatus == "Completed"),

                TotalRevenue =
                    allOrders
                        .Where(o => o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
            });
        }

        // =========================================================
        // ORDER DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var order = await _context.orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == id &&
                        o.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (order == null)
            {
                return NotFound();
            }

            var customer = await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CustomerId == order.CustomerId,
                    cancellationToken);

            User? customerUser = null;

            if (customer != null)
            {
                customerUser = await _context.users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.UserId == customer.UserId,
                        cancellationToken);
            }

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == order.FarmerMarketId &&
                        fm.FarmerId == farmer.FarmerId,
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

            PickupSlot? pickupSlot = null;

            if (order.PickupSlotId.HasValue)
            {
                pickupSlot = await _context.pickupslots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        ps => ps.PickupSlotId == order.PickupSlotId.Value,
                        cancellationToken);
            }

            var items = await _context.orderitems
                .AsNoTracking()
                .Where(oi => oi.OrderId == order.OrderId)
                .OrderBy(oi => oi.OrderItemId)
                .Select(oi => new FarmerOrderItemViewModel
                {
                    OrderItemId = oi.OrderItemId,
                    ProductName = oi.ProductName,
                    UnitName = oi.UnitName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                })
                .ToListAsync(cancellationToken);

            var historyRows = await _context.orderstatushistories
                .AsNoTracking()
                .Where(h => h.OrderId == order.OrderId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync(cancellationToken);

            var changedByUserIds = historyRows
                .Where(h => h.ChangedByUserId.HasValue)
                .Select(h => h.ChangedByUserId!.Value)
                .Distinct()
                .ToList();

            var changedByUsers = await _context.users
                .AsNoTracking()
                .Where(u => changedByUserIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, cancellationToken);

            var history = historyRows.Select(h =>
            {
                var changedBy = "System";

                if (h.ChangedByUserId.HasValue &&
                    changedByUsers.TryGetValue(
                        h.ChangedByUserId.Value,
                        out var user))
                {
                    changedBy = user.FullName;
                }

                return new FarmerOrderHistoryViewModel
                {
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    Remarks = h.Remarks,
                    ChangedAt = h.ChangedAt,
                    ChangedByName = changedBy
                };
            }).ToList();

            return View(new FarmerOrderDetailsViewModel
            {
                OrderId = order.OrderId,
                OrderNo = order.OrderNo,
                OrderStatus = order.OrderStatus,

                CustomerName =
                    customerUser?.FullName ?? "Customer",

                CustomerEmail =
                    customerUser?.Email ?? "",

                CustomerPhone =
                    customerUser?.Phone ?? "",

                MarketName =
                    market?.MarketName ?? "Market",

                StallNumber =
                    farmerMarket?.StallNumber,

                PickupInstructions =
                    farmerMarket?.PickupInstructions,

                PickupDate = order.PickupDate,

                PickupStartTime =
                    pickupSlot?.StartTime,

                PickupEndTime =
                    pickupSlot?.EndTime,

                TotalAmount = order.TotalAmount,

                CustomerNotes = order.CustomerNotes,
                FarmerNotes = order.FarmerNotes,

                OrderDate = order.OrderDate,
                CompletedAt = order.CompletedAt,

                Items = items,
                History = history
            });
        }

        // =========================================================
        // ACCEPT
        // Placed -> Accepted
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(
            UpdateFarmerOrderStatusViewModel model,
            CancellationToken cancellationToken)
        {
            return await ChangeStatusAsync(
                model,
                expectedStatus: "Placed",
                newStatus: "Accepted",
                defaultRemarks: "Order accepted by farmer.",
                cancellationToken);
        }

        // =========================================================
        // DECLINE
        // Placed -> Declined
        // Also releases reserved inventory and pickup capacity.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(
            UpdateFarmerOrderStatusViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var order = await _context.orders
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == model.OrderId &&
                        o.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Placed")
            {
                TempData["ErrorMessage"] =
                    "Only newly placed orders can be declined.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var items = await _context.orderitems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .ToListAsync(cancellationToken);

                foreach (var item in items)
                {
                    if (!item.InventoryId.HasValue)
                    {
                        continue;
                    }

                    var inventory = await _context.inventories
                        .FirstOrDefaultAsync(
                            i => i.InventoryId == item.InventoryId.Value,
                            cancellationToken);

                    if (inventory == null)
                    {
                        continue;
                    }

                    inventory.ReservedQuantity =
                        Math.Max(
                            0,
                            inventory.ReservedQuantity -
                            item.Quantity);

                    var available =
                        inventory.StockQuantity -
                        inventory.ReservedQuantity -
                        inventory.SoldQuantity;

                    inventory.IsSoldOut =
                        available <= 0;

                    if (available > 0)
                    {
                        inventory.IsAvailable = true;
                    }

                    inventory.UpdatedAt = DateTime.Now;
                }

                if (order.PickupSlotId.HasValue)
                {
                    var slot = await _context.pickupslots
                        .FirstOrDefaultAsync(
                            ps =>
                                ps.PickupSlotId ==
                                order.PickupSlotId.Value,
                            cancellationToken);

                    if (slot != null)
                    {
                        slot.BookedOrders =
                            Math.Max(
                                0,
                                slot.BookedOrders - 1);

                        if (slot.BookedOrders <
                            slot.MaximumOrders)
                        {
                            slot.IsAvailable = true;
                        }
                    }
                }

                var previousStatus = order.OrderStatus;

                order.OrderStatus = "Declined";
                order.FarmerNotes =
                    CleanNullable(model.FarmerNotes);

                AddStatusHistory(
                    order.OrderId,
                    previousStatus,
                    "Declined",
                    farmer.UserId,
                    string.IsNullOrWhiteSpace(model.FarmerNotes)
                        ? "Order declined by farmer."
                        : model.FarmerNotes.Trim());

                await AddCustomerNotificationAsync(
                    order,
                    "Order Declined",
                    $"Your order {order.OrderNo} was declined by the farmer.",
                    "OrderUpdate",
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Order declined and reserved stock released.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                TempData["ErrorMessage"] =
                    "The order could not be declined.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }
        }

        // =========================================================
        // READY
        // Accepted -> Ready for Pickup
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReady(
            UpdateFarmerOrderStatusViewModel model,
            CancellationToken cancellationToken)
        {
            return await ChangeStatusAsync(
                model,
                expectedStatus: "Accepted",
                newStatus: "Ready for Pickup",
                defaultRemarks: "Order is ready for pickup.",
                cancellationToken);
        }

        // =========================================================
        // COMPLETE
        // Ready for Pickup -> Completed
        // Reserved quantity becomes sold quantity.
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            UpdateFarmerOrderStatusViewModel model,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var order = await _context.orders
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == model.OrderId &&
                        o.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Ready for Pickup")
            {
                TempData["ErrorMessage"] =
                    "Only orders ready for pickup can be completed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var items = await _context.orderitems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .ToListAsync(cancellationToken);

                foreach (var item in items)
                {
                    if (!item.InventoryId.HasValue)
                    {
                        continue;
                    }

                    var inventory = await _context.inventories
                        .FirstOrDefaultAsync(
                            i => i.InventoryId == item.InventoryId.Value,
                            cancellationToken);

                    if (inventory == null)
                    {
                        continue;
                    }

                    inventory.ReservedQuantity =
                        Math.Max(
                            0,
                            inventory.ReservedQuantity -
                            item.Quantity);

                    inventory.SoldQuantity +=
                        item.Quantity;

                    var available =
                        inventory.StockQuantity -
                        inventory.ReservedQuantity -
                        inventory.SoldQuantity;

                    inventory.IsSoldOut =
                        available <= 0;

                    inventory.IsAvailable =
                        available > 0 &&
                        inventory.IsAvailable;

                    inventory.UpdatedAt =
                        DateTime.Now;
                }

                var previousStatus = order.OrderStatus;

                order.OrderStatus = "Completed";
                order.CompletedAt = DateTime.Now;
                order.FarmerNotes =
                    CleanNullable(model.FarmerNotes)
                    ?? order.FarmerNotes;

                AddStatusHistory(
                    order.OrderId,
                    previousStatus,
                    "Completed",
                    farmer.UserId,
                    string.IsNullOrWhiteSpace(model.FarmerNotes)
                        ? "Order completed after pickup."
                        : model.FarmerNotes.Trim());

                await AddCustomerNotificationAsync(
                    order,
                    "Order Completed",
                    $"Your order {order.OrderNo} has been completed. Thank you!",
                    "OrderUpdate",
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Order completed and inventory updated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                TempData["ErrorMessage"] =
                    "The order could not be completed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }
        }

        // =========================================================
        // COMMON STATUS CHANGE
        // =========================================================
        private async Task<IActionResult> ChangeStatusAsync(
            UpdateFarmerOrderStatusViewModel model,
            string expectedStatus,
            string newStatus,
            string defaultRemarks,
            CancellationToken cancellationToken)
        {
            var farmer = await GetCurrentFarmerAsync(cancellationToken);

            if (farmer == null)
            {
                return Forbid();
            }

            var order = await _context.orders
                .FirstOrDefaultAsync(
                    o =>
                        o.OrderId == model.OrderId &&
                        o.FarmerId == farmer.FarmerId,
                    cancellationToken);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != expectedStatus)
            {
                TempData["ErrorMessage"] =
                    $"This action is only available when the order is {expectedStatus}.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.OrderId });
            }

            var previousStatus = order.OrderStatus;

            order.OrderStatus = newStatus;

            if (!string.IsNullOrWhiteSpace(model.FarmerNotes))
            {
                order.FarmerNotes =
                    model.FarmerNotes.Trim();
            }

            AddStatusHistory(
                order.OrderId,
                previousStatus,
                newStatus,
                farmer.UserId,
                string.IsNullOrWhiteSpace(model.FarmerNotes)
                    ? defaultRemarks
                    : model.FarmerNotes.Trim());

            var title =
                newStatus == "Accepted"
                    ? "Order Accepted"
                    : newStatus == "Ready for Pickup"
                        ? "Order Ready for Pickup"
                        : "Order Updated";

            var message =
                newStatus == "Accepted"
                    ? $"Your order {order.OrderNo} has been accepted."
                    : newStatus == "Ready for Pickup"
                        ? $"Your order {order.OrderNo} is ready for pickup."
                        : $"Your order {order.OrderNo} status is now {newStatus}.";

            await AddCustomerNotificationAsync(
                order,
                title,
                message,
                "OrderUpdate",
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"Order status changed to {newStatus}.";

            return RedirectToAction(
                nameof(Details),
                new { id = order.OrderId });
        }

        private void AddStatusHistory(
            int orderId,
            string previousStatus,
            string newStatus,
            int changedByUserId,
            string remarks)
        {
            _context.orderstatushistories.Add(
                new OrderStatusHistory
                {
                    OrderId = orderId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    ChangedByUserId = changedByUserId,
                    Remarks = remarks,
                    ChangedAt = DateTime.Now
                });
        }

        private async Task AddCustomerNotificationAsync(
            Order order,
            string title,
            string message,
            string notificationType,
            CancellationToken cancellationToken)
        {
            var customer = await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.CustomerId == order.CustomerId,
                    cancellationToken);

            if (customer == null)
            {
                return;
            }

            _context.notifications.Add(
                new Notification
                {
                    UserId = customer.UserId,
                    Title = title,
                    Message = message,
                    NotificationType = notificationType,
                    ActionUrl = $"/CustomerOrders/Details/{order.OrderId}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
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

        private static string? CleanNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
