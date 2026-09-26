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
    public class CustomerCartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerCartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var cart = await _context.carts
                .AsNoTracking()
                .Where(c =>
                    c.CustomerId == customer.CustomerId &&
                    c.IsActive)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cart == null)
            {
                return View(new CustomerCartPageViewModel());
            }

            var vm = await BuildCartPageAsync(
                cart,
                cancellationToken);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            AddToCartViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Please enter a valid quantity.";

                return RedirectBackOrCart(model.ReturnUrl);
            }

            var farmerProduct = await _context.farmerproducts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fp =>
                        fp.FarmerProductId == model.FarmerProductId &&
                        fp.IsApproved &&
                        fp.IsActive &&
                        fp.IsAvailable,
                    cancellationToken);

            if (farmerProduct == null)
            {
                TempData["ErrorMessage"] =
                    "This product is not currently available.";

                return RedirectBackOrCart(model.ReturnUrl);
            }

            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f =>
                        f.FarmerId == farmerProduct.FarmerId &&
                        f.IsApproved &&
                        f.IsActive,
                    cancellationToken);

            if (farmer == null)
            {
                TempData["ErrorMessage"] =
                    "This farmer is not currently available.";

                return RedirectBackOrCart(model.ReturnUrl);
            }

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm =>
                        fm.FarmerMarketId == model.FarmerMarketId &&
                        fm.FarmerId == farmerProduct.FarmerId &&
                        fm.IsActive,
                    cancellationToken);

            if (farmerMarket == null)
            {
                TempData["ErrorMessage"] =
                    "The selected pickup market is not available for this farmer.";

                return RedirectBackOrCart(model.ReturnUrl);
            }

            var availableQuantity = await GetAvailableQuantityAsync(
                farmerProduct.FarmerProductId,
                farmerMarket.FarmerMarketId,
                cancellationToken);

            if (availableQuantity <= 0)
            {
                TempData["ErrorMessage"] =
                    "This product is sold out at the selected market.";

                return RedirectBackOrCart(model.ReturnUrl);
            }

            var activeCart = await _context.carts
                .FirstOrDefaultAsync(
                    c =>
                        c.CustomerId == customer.CustomerId &&
                        c.IsActive,
                    cancellationToken);

            if (activeCart != null &&
                (activeCart.FarmerId != farmerProduct.FarmerId ||
                 activeCart.FarmerMarketId != farmerMarket.FarmerMarketId))
            {
                TempData["ErrorMessage"] =
                    "Your cart already contains products from another farmer or market. " +
                    "Please checkout or clear the current cart first.";

                return RedirectToAction(nameof(Index));
            }

            if (activeCart == null)
            {
                activeCart = new Cart
                {
                    CustomerId = customer.CustomerId,
                    FarmerId = farmerProduct.FarmerId,
                    FarmerMarketId = farmerMarket.FarmerMarketId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.carts.Add(activeCart);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var item = await _context.cartitems
                .FirstOrDefaultAsync(
                    ci =>
                        ci.CartId == activeCart.CartId &&
                        ci.FarmerProductId == farmerProduct.FarmerProductId,
                    cancellationToken);

            var requestedQuantity =
                model.Quantity +
                (item?.Quantity ?? 0);

            if (requestedQuantity > availableQuantity)
            {
                TempData["ErrorMessage"] =
                    $"Only {availableQuantity:N3} is currently available at this market.";

                return RedirectBackOrCart(model.ReturnUrl);
            }

            if (item == null)
            {
                item = new CartItem
                {
                    CartId = activeCart.CartId,
                    FarmerProductId = farmerProduct.FarmerProductId,
                    Quantity = model.Quantity,
                    UnitPrice = farmerProduct.Price,
                    AddedAt = DateTime.Now
                };

                _context.cartitems.Add(item);
            }
            else
            {
                item.Quantity = requestedQuantity;
                item.UnitPrice = farmerProduct.Price;
            }

            activeCart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Product added to your cart.";

            return RedirectBackOrCart(model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            UpdateCartItemViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Please enter a valid quantity.";

                return RedirectToAction(nameof(Index));
            }

            var cartItem = await _context.cartitems
                .FirstOrDefaultAsync(
                    ci => ci.CartItemId == model.CartItemId,
                    cancellationToken);

            if (cartItem == null)
                return NotFound();

            var cart = await _context.carts
                .FirstOrDefaultAsync(
                    c =>
                        c.CartId == cartItem.CartId &&
                        c.CustomerId == customer.CustomerId &&
                        c.IsActive,
                    cancellationToken);

            if (cart == null)
                return NotFound();

            var availableQuantity = await GetAvailableQuantityAsync(
                cartItem.FarmerProductId,
                cart.FarmerMarketId,
                cancellationToken);

            if (model.Quantity > availableQuantity)
            {
                TempData["ErrorMessage"] =
                    $"Only {availableQuantity:N3} is currently available.";

                return RedirectToAction(nameof(Index));
            }

            cartItem.Quantity = model.Quantity;
            cart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Cart quantity updated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(
            int id,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var cartItem = await _context.cartitems
                .FirstOrDefaultAsync(
                    ci => ci.CartItemId == id,
                    cancellationToken);

            if (cartItem == null)
                return NotFound();

            var cart = await _context.carts
                .FirstOrDefaultAsync(
                    c =>
                        c.CartId == cartItem.CartId &&
                        c.CustomerId == customer.CustomerId &&
                        c.IsActive,
                    cancellationToken);

            if (cart == null)
                return NotFound();

            _context.cartitems.Remove(cartItem);

            var remainingItems = await _context.cartitems
                .CountAsync(
                    ci =>
                        ci.CartId == cart.CartId &&
                        ci.CartItemId != id,
                    cancellationToken);

            if (remainingItems == 0)
            {
                cart.IsActive = false;
            }

            cart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Product removed from cart.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear(
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var cart = await _context.carts
                .FirstOrDefaultAsync(
                    c =>
                        c.CustomerId == customer.CustomerId &&
                        c.IsActive,
                    cancellationToken);

            if (cart == null)
                return RedirectToAction(nameof(Index));

            cart.IsActive = false;
            cart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] =
                "Cart cleared.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var cart = await _context.carts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c =>
                        c.CustomerId == customer.CustomerId &&
                        c.IsActive,
                    cancellationToken);

            if (cart == null)
            {
                TempData["ErrorMessage"] =
                    "Your cart is empty.";

                return RedirectToAction(nameof(Index));
            }

            var model = await BuildCheckoutAsync(
                cart,
                null,
                cancellationToken);

            if (model.Items.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "Your cart is empty.";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            CustomerCheckoutViewModel model,
            CancellationToken cancellationToken)
        {
            var customer = await GetCurrentCustomerAsync(cancellationToken);

            if (customer == null)
                return Forbid();

            var cart = await _context.carts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c =>
                        c.CartId == model.CartId &&
                        c.CustomerId == customer.CustomerId &&
                        c.IsActive,
                    cancellationToken);

            if (cart == null)
            {
                TempData["ErrorMessage"] =
                    "Your cart is no longer active.";

                return RedirectToAction(nameof(Index));
            }

            if (!model.PickupSlotId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.PickupSlotId),
                    "Please choose a pickup slot.");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildCheckoutAsync(
                    cart,
                    model,
                    cancellationToken);

                return View(invalidModel);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                var trackedCart = await _context.carts
                    .FirstOrDefaultAsync(
                        c =>
                            c.CartId == cart.CartId &&
                            c.CustomerId == customer.CustomerId &&
                            c.IsActive,
                        cancellationToken);

                if (trackedCart == null)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["ErrorMessage"] =
                        "Your cart is no longer active.";

                    return RedirectToAction(nameof(Index));
                }

                var cartItems = await _context.cartitems
                    .Where(ci => ci.CartId == trackedCart.CartId)
                    .ToListAsync(cancellationToken);

                if (cartItems.Count == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["ErrorMessage"] =
                        "Your cart is empty.";

                    return RedirectToAction(nameof(Index));
                }

                var pickupSlot = await _context.pickupslots
                    .FirstOrDefaultAsync(
                        ps =>
                            ps.PickupSlotId == model.PickupSlotId.Value &&
                            ps.FarmerMarketId == trackedCart.FarmerMarketId,
                        cancellationToken);

                if (pickupSlot == null ||
                    !pickupSlot.IsAvailable ||
                    pickupSlot.BookedOrders >= pickupSlot.MaximumOrders ||
                    pickupSlot.PickupDate.Date < DateTime.Today)
                {
                    ModelState.AddModelError(
                        nameof(model.PickupSlotId),
                        "The selected pickup slot is no longer available.");

                    await transaction.RollbackAsync(cancellationToken);

                    var invalidModel = await BuildCheckoutAsync(
                        cart,
                        model,
                        cancellationToken);

                    return View(invalidModel);
                }

                var farmerMarket = await _context.farmermarkets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        fm =>
                            fm.FarmerMarketId == trackedCart.FarmerMarketId &&
                            fm.FarmerId == trackedCart.FarmerId &&
                            fm.IsActive,
                        cancellationToken);

                if (farmerMarket == null)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["ErrorMessage"] =
                        "The selected market is no longer active.";

                    return RedirectToAction(nameof(Index));
                }

                var marketDay = await _context.marketdays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        md =>
                            md.MarketId == farmerMarket.MarketId &&
                            md.IsActive &&
                            md.DayName == pickupSlot.PickupDate.DayOfWeek.ToString(),
                        cancellationToken);

                if (marketDay == null)
                {
                    ModelState.AddModelError(
                        nameof(model.PickupSlotId),
                        "The selected pickup date is not an active market day.");

                    await transaction.RollbackAsync(cancellationToken);

                    var invalidModel = await BuildCheckoutAsync(
                        cart,
                        model,
                        cancellationToken);

                    return View(invalidModel);
                }

                var farmerMarketDay = await _context.farmermarketdays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        fmd =>
                            fmd.FarmerMarketId == farmerMarket.FarmerMarketId &&
                            fmd.MarketDayId == marketDay.MarketDayId &&
                            fmd.IsActive,
                        cancellationToken);

                if (farmerMarketDay == null)
                {
                    ModelState.AddModelError(
                        nameof(model.PickupSlotId),
                        "The farmer is not taking orders for this market day.");

                    await transaction.RollbackAsync(cancellationToken);

                    var invalidModel = await BuildCheckoutAsync(
                        cart,
                        model,
                        cancellationToken);

                    return View(invalidModel);
                }

                var cutoffDateTime =
                    pickupSlot.PickupDate.Date +
                    farmerMarketDay.OrderCutoffTime;

                if (DateTime.Now > cutoffDateTime)
                {
                    ModelState.AddModelError(
                        nameof(model.PickupSlotId),
                        "The order cutoff time has passed for this pickup slot.");

                    await transaction.RollbackAsync(cancellationToken);

                    var invalidModel = await BuildCheckoutAsync(
                        cart,
                        model,
                        cancellationToken);

                    return View(invalidModel);
                }

                var farmerProductIds = cartItems
                    .Select(ci => ci.FarmerProductId)
                    .Distinct()
                    .ToList();

                var farmerProducts = await _context.farmerproducts
                    .AsNoTracking()
                    .Where(fp =>
                        farmerProductIds.Contains(fp.FarmerProductId) &&
                        fp.FarmerId == trackedCart.FarmerId &&
                        fp.IsApproved &&
                        fp.IsActive)
                    .ToDictionaryAsync(
                        fp => fp.FarmerProductId,
                        cancellationToken);

                var productIds = farmerProducts.Values
                    .Select(fp => fp.ProductId)
                    .Distinct()
                    .ToList();

                var products = await _context.products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(
                        p => p.ProductId,
                        cancellationToken);

                var unitIds = farmerProducts.Values
                    .Select(fp => fp.UnitOfMeasureId)
                    .Distinct()
                    .ToList();

                var units = await _context.unitofmeasures
                    .AsNoTracking()
                    .Where(u => unitIds.Contains(u.UnitOfMeasureId))
                    .ToDictionaryAsync(
                        u => u.UnitOfMeasureId,
                        cancellationToken);

                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                foreach (var cartItem in cartItems)
                {
                    if (!farmerProducts.TryGetValue(
                            cartItem.FarmerProductId,
                            out var fp))
                    {
                        ModelState.AddModelError(
                            "",
                            "One of the products is no longer available.");

                        await transaction.RollbackAsync(cancellationToken);

                        var invalidModel = await BuildCheckoutAsync(
                            cart,
                            model,
                            cancellationToken);

                        return View(invalidModel);
                    }

                    var inventoryCandidates = await _context.inventories
                        .Where(i =>
                            i.FarmerProductId == cartItem.FarmerProductId &&
                            i.FarmerMarketId == trackedCart.FarmerMarketId &&
                            i.InventoryDate.Date == pickupSlot.PickupDate.Date &&
                            i.IsAvailable &&
                            !i.IsSoldOut)
                        .OrderBy(i => i.InventoryId)
                        .ToListAsync(cancellationToken);

                    var inventory = inventoryCandidates
                        .FirstOrDefault(i =>
                            i.StockQuantity -
                            i.ReservedQuantity -
                            i.SoldQuantity >= cartItem.Quantity);

                    if (inventory == null)
                    {
                        ModelState.AddModelError(
                            "",
                            $"Insufficient stock is available for one of the cart items on {pickupSlot.PickupDate:dd MMM yyyy}.");

                        await transaction.RollbackAsync(cancellationToken);

                        var invalidModel = await BuildCheckoutAsync(
                            cart,
                            model,
                            cancellationToken);

                        return View(invalidModel);
                    }

                    var product = products[fp.ProductId];
                    units.TryGetValue(fp.UnitOfMeasureId, out var unit);

                    var unitPrice = inventory.UnitPrice;
                    var lineTotal = unitPrice * cartItem.Quantity;

                    totalAmount += lineTotal;

                    inventory.ReservedQuantity += cartItem.Quantity;

                    var remaining =
                        inventory.StockQuantity -
                        inventory.ReservedQuantity -
                        inventory.SoldQuantity;

                    if (remaining <= 0)
                    {
                        inventory.IsSoldOut = true;
                        inventory.IsAvailable = false;
                    }

                    orderItems.Add(
                        new OrderItem
                        {
                            FarmerProductId = fp.FarmerProductId,
                            InventoryId = inventory.InventoryId,
                            ProductName = product.ProductName,
                            UnitName =
                                unit?.UnitCode ??
                                unit?.UnitName ??
                                "",
                            Quantity = cartItem.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = lineTotal
                        });
                }

                var orderNo =
                    await GenerateOrderNoAsync(cancellationToken);

                var order = new Order
                {
                    OrderNo = orderNo,
                    CustomerId = customer.CustomerId,
                    FarmerId = trackedCart.FarmerId,
                    FarmerMarketId = trackedCart.FarmerMarketId,
                    PickupSlotId = pickupSlot.PickupSlotId,
                    PickupDate = pickupSlot.PickupDate.Date,
                    TotalAmount = totalAmount,
                    OrderStatus = "Placed",
                    CustomerNotes =
                        string.IsNullOrWhiteSpace(model.CustomerNotes)
                            ? null
                            : model.CustomerNotes.Trim(),
                    OrderDate = DateTime.Now
                };

                _context.orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var item in orderItems)
                {
                    item.OrderId = order.OrderId;
                    _context.orderitems.Add(item);
                }

                _context.orderstatushistories.Add(
                    new OrderStatusHistory
                    {
                        OrderId = order.OrderId,
                        PreviousStatus = "",
                        NewStatus = "Placed",
                        ChangedByUserId = customer.UserId,
                        Remarks = "Order placed by customer.",
                        ChangedAt = DateTime.Now
                    });

                pickupSlot.BookedOrders += 1;

                if (pickupSlot.BookedOrders >= pickupSlot.MaximumOrders)
                {
                    pickupSlot.IsAvailable = false;
                }

                var farmer = await _context.farmers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        f => f.FarmerId == trackedCart.FarmerId,
                        cancellationToken);

                if (farmer != null)
                {
                    _context.notifications.Add(
                        new Notification
                        {
                            UserId = farmer.UserId,
                            Title = "New Order Placed",
                            Message =
                                $"New order {order.OrderNo} has been placed for pickup on {order.PickupDate:dd MMM yyyy}.",
                            NotificationType = "OrderUpdate",
                            ActionUrl =
                                $"/FarmerOrders/Details/{order.OrderId}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                }

                trackedCart.IsActive = false;
                trackedCart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["SuccessMessage"] =
                    "Your pre-order has been placed successfully.";

                return RedirectToAction(
                    nameof(Success),
                    new { id = order.OrderId });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(
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

            return View(new CustomerOrderSuccessViewModel
            {
                OrderId = order.OrderId,
                OrderNo = order.OrderNo,
                MarketName = market?.MarketName ?? "Market",
                PickupDate = order.PickupDate,
                PickupTimeText =
                    slot == null
                        ? ""
                        : $"{DateTime.Today.Add(slot.StartTime):hh:mm tt} - " +
                          $"{DateTime.Today.Add(slot.EndTime):hh:mm tt}",
                TotalAmount = order.TotalAmount
            });
        }

        private async Task<CustomerCartPageViewModel> BuildCartPageAsync(
            Cart cart,
            CancellationToken cancellationToken)
        {
            var farmer = await _context.farmers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FarmerId == cart.FarmerId,
                    cancellationToken);

            var farmerMarket = await _context.farmermarkets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    fm => fm.FarmerMarketId == cart.FarmerMarketId,
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

            var cartItems = await _context.cartitems
                .AsNoTracking()
                .Where(ci => ci.CartId == cart.CartId)
                .ToListAsync(cancellationToken);

            var farmerProductIds = cartItems
                .Select(ci => ci.FarmerProductId)
                .Distinct()
                .ToList();

            var farmerProducts = await _context.farmerproducts
                .AsNoTracking()
                .Where(fp => farmerProductIds.Contains(fp.FarmerProductId))
                .ToDictionaryAsync(
                    fp => fp.FarmerProductId,
                    cancellationToken);

            var productIds = farmerProducts.Values
                .Select(fp => fp.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(
                    p => p.ProductId,
                    cancellationToken);

            var unitIds = farmerProducts.Values
                .Select(fp => fp.UnitOfMeasureId)
                .Distinct()
                .ToList();

            var units = await _context.unitofmeasures
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.UnitOfMeasureId))
                .ToDictionaryAsync(
                    u => u.UnitOfMeasureId,
                    cancellationToken);

            var images = await _context.productimages
                .AsNoTracking()
                .Where(pi => farmerProductIds.Contains(pi.FarmerProductId))
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync(cancellationToken);

            var rows = new List<CustomerCartItemViewModel>();

            foreach (var item in cartItems)
            {
                if (!farmerProducts.TryGetValue(
                        item.FarmerProductId,
                        out var fp))
                    continue;

                products.TryGetValue(fp.ProductId, out var product);
                units.TryGetValue(fp.UnitOfMeasureId, out var unit);

                var image = images.FirstOrDefault(
                    i => i.FarmerProductId == fp.FarmerProductId);

                var available = await GetAvailableQuantityAsync(
                    fp.FarmerProductId,
                    cart.FarmerMarketId,
                    cancellationToken);

                rows.Add(new CustomerCartItemViewModel
                {
                    CartItemId = item.CartItemId,
                    FarmerProductId = fp.FarmerProductId,
                    ProductName = product?.ProductName ?? "Product",
                    UnitName = unit?.UnitCode ?? unit?.UnitName ?? "",
                    ImageUrl = image?.ImageUrl ?? product?.DefaultImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    AvailableQuantity = available
                });
            }

            return new CustomerCartPageViewModel
            {
                CartId = cart.CartId,
                FarmerId = cart.FarmerId,
                FarmerMarketId = cart.FarmerMarketId,
                FarmerName = farmer?.BusinessName ?? "Farmer",
                MarketName = market?.MarketName ?? "Market",
                MarketAddress = market?.Address ?? "",
                StallNumber = farmerMarket?.StallNumber,
                Items = rows
            };
        }

        private async Task<CustomerCheckoutViewModel> BuildCheckoutAsync(
            Cart cart,
            CustomerCheckoutViewModel? input,
            CancellationToken cancellationToken)
        {
            var cartPage = await BuildCartPageAsync(
                cart,
                cancellationToken);

            var cartItems = await _context.cartitems
                .AsNoTracking()
                .Where(ci => ci.CartId == cart.CartId)
                .ToListAsync(cancellationToken);

            var availableSlots = await _context.pickupslots
                .AsNoTracking()
                .Where(ps =>
                    ps.FarmerMarketId == cart.FarmerMarketId &&
                    ps.IsAvailable &&
                    ps.PickupDate.Date >= DateTime.Today &&
                    ps.BookedOrders < ps.MaximumOrders)
                .OrderBy(ps => ps.PickupDate)
                .ThenBy(ps => ps.StartTime)
                .ToListAsync(cancellationToken);

            var eligibleSlots = new List<CustomerPickupSlotOptionViewModel>();

            foreach (var slot in availableSlots)
            {
                var allItemsAvailable = true;

                foreach (var cartItem in cartItems)
                {
                    var enoughStock = await _context.inventories
                        .AsNoTracking()
                        .AnyAsync(
                            i =>
                                i.FarmerProductId == cartItem.FarmerProductId &&
                                i.FarmerMarketId == cart.FarmerMarketId &&
                                i.InventoryDate.Date == slot.PickupDate.Date &&
                                i.IsAvailable &&
                                !i.IsSoldOut &&
                                i.StockQuantity -
                                i.ReservedQuantity -
                                i.SoldQuantity >= cartItem.Quantity,
                            cancellationToken);

                    if (!enoughStock)
                    {
                        allItemsAvailable = false;
                        break;
                    }
                }

                if (allItemsAvailable)
                {
                    eligibleSlots.Add(
                        new CustomerPickupSlotOptionViewModel
                        {
                            PickupSlotId = slot.PickupSlotId,
                            PickupDate = slot.PickupDate,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            MaximumOrders = slot.MaximumOrders,
                            BookedOrders = slot.BookedOrders
                        });
                }
            }

            return new CustomerCheckoutViewModel
            {
                CartId = cart.CartId,
                PickupSlotId = input?.PickupSlotId,
                CustomerNotes = input?.CustomerNotes,
                FarmerName = cartPage.FarmerName,
                MarketName = cartPage.MarketName,
                MarketAddress = cartPage.MarketAddress,
                Items = cartPage.Items.Select(i =>
                    new CustomerCheckoutItemViewModel
                    {
                        FarmerProductId = i.FarmerProductId,
                        ProductName = i.ProductName,
                        UnitName = i.UnitName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                PickupSlots = eligibleSlots
            };
        }

        private async Task<decimal> GetAvailableQuantityAsync(
            int farmerProductId,
            int farmerMarketId,
            CancellationToken cancellationToken)
        {
            var inventory = await _context.inventories
                .AsNoTracking()
                .Where(i =>
                    i.FarmerProductId == farmerProductId &&
                    i.FarmerMarketId == farmerMarketId &&
                    i.InventoryDate >= DateTime.Today &&
                    i.IsAvailable &&
                    !i.IsSoldOut)
                .ToListAsync(cancellationToken);

            return inventory.Sum(i =>
                Math.Max(
                    0,
                    i.StockQuantity -
                    i.ReservedQuantity -
                    i.SoldQuantity));
        }

        private async Task<string> GenerateOrderNoAsync(
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var value =
                    $"ML-{DateTime.Now:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

                var exists = await _context.orders
                    .AsNoTracking()
                    .AnyAsync(
                        o => o.OrderNo == value,
                        cancellationToken);

                if (!exists)
                    return value;
            }

            return $"ML-{Guid.NewGuid().ToString("N")[..20]}";
        }

        private IActionResult RedirectBackOrCart(
            string? returnUrl)
        {
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
                return null;

            return await _context.customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.UserId == userId,
                    cancellationToken);
        }
    }
}
