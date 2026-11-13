using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync(Guid userId, Guid? sellerId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .WhereIf(sellerId, o => o.SellerId == sellerId!.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetForSellerAsync(Guid sellerUserId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Seller.UserId == sellerUserId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .Include(o => o.PromoCode)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
    }

    public async Task<Order> CreateAsync(Guid userId, CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SellerId = request.SellerId,
            PromoCodeId = request.PromoCodeId,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var totalAmount = 0m;
        foreach (var itemRequest in request.Items)
        {
            var product = await _context.Products
                .Include(p => p.Galleries)
                .FirstOrDefaultAsync(p => p.Id == itemRequest.ProductId && p.IsActive);

            if (product == null)
                throw new InvalidOperationException($"Product {itemRequest.ProductId} not found or inactive");

            if (product.Stock < itemRequest.Quantity)
                throw new InvalidOperationException($"Not enough stock for product {product.Name}");

            var avatarUrl = product.Galleries
                .OrderByDescending(g => g.IsMain)
                .Select(g => g.Path)
                .FirstOrDefault() ?? string.Empty;

            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductPrice = product.Price,
                ProductAvatarUrl = avatarUrl,
                Quantity = itemRequest.Quantity,
                TotalPrice = product.Price * itemRequest.Quantity
            };

            totalAmount += item.TotalPrice;
            order.Items.Add(item);

            product.Stock -= itemRequest.Quantity;
        }

        order.TotalAmount = totalAmount;

        var discount = await _context.ApplyPromoCodeAsync(request.PromoCodeId, totalAmount, userId);
        if (discount != null)
            order.DiscountAmount = discount.DiscountAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<List<Order>> GetAllForAdminAsync(Guid? sellerId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .WhereIf(sellerId, o => o.SellerId == sellerId!.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<CheckoutResponse> CheckoutAsync(Guid userId, CheckoutRequest request)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Seller)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Galleries)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");

        // One line per product (the cart may hold the same product twice)
        var lines = cart.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (Product: g.First().Product, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        // Same rule as the storefront: active product of an approved seller
        foreach (var (product, quantity) in lines)
        {
            if (product is null || !product.IsActive || product.Seller?.Status != SellerStatus.Active)
            {
                var name = product?.Name ?? "A product";
                throw new InvalidOperationException($"\"{name}\" is no longer available. Remove it from your cart to continue.");
            }

            if (product.Stock <= 0)
                throw new InvalidOperationException($"\"{product.Name}\" is out of stock.");

            if (product.Stock < quantity)
                throw new InvalidOperationException($"Only {product.Stock} left of \"{product.Name}\".");
        }

        var subtotal = lines.Sum(l => l.Product.Price * l.Quantity);

        PromoCode? promo = null;
        var discount = 0m;
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            promo = await ValidatePromoCodeAsync(request.PromoCode.Trim(), subtotal, userId);
            discount = CalculateDiscount(promo, subtotal);
        }

        // All orders of one checkout share CreatedAt; promo usage per user is counted per checkout
        var now = DateTime.UtcNow;
        var groups = lines.GroupBy(l => l.Product.SellerId).ToList();
        var orders = new List<Order>();
        var discountLeft = discount;

        for (var index = 0; index < groups.Count; index++)
        {
            var group = groups[index];
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SellerId = group.Key,
                PromoCodeId = promo?.Id,
                Status = OrderStatus.Pending,
                ShippingAddress = request.ShippingAddress.Trim(),
                ReceiverName = request.ReceiverName.Trim(),
                ReceiverPhone = request.ReceiverPhone.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var (product, quantity) in group)
            {
                var avatarUrl = product.Galleries
                    .OrderByDescending(g => g.IsMain)
                    .Select(g => g.Path)
                    .FirstOrDefault() ?? string.Empty;

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductPrice = product.Price,
                    ProductAvatarUrl = avatarUrl,
                    Quantity = quantity,
                    TotalPrice = product.Price * quantity
                });

                product.Stock -= quantity;
            }

            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // The discount is split by order amount; the last order takes the rounding remainder
            var share = index == groups.Count - 1
                ? discountLeft
                : Math.Round(discount * order.TotalAmount / subtotal, 2, MidpointRounding.AwayFromZero);
            share = Math.Clamp(share, 0m, Math.Min(order.TotalAmount, discountLeft));
            order.DiscountAmount = share;
            discountLeft -= share;

            orders.Add(order);
        }

        if (promo is not null)
            promo.UsedActivationsCount++;

        _context.Orders.AddRange(orders);
        _context.CartItems.RemoveRange(cart.Items);

        // One SaveChanges: orders, stock, promo usage and the emptied cart are committed
        // together (a single database transaction on PostgreSQL)
        await _context.SaveChangesAsync();

        return new CheckoutResponse(
            orders.Select(o => new CheckoutOrderSummary(
                o.Id,
                o.SellerId,
                lines.First(l => l.Product.SellerId == o.SellerId).Product.Seller.StoreName,
                o.Items.Sum(i => i.Quantity),
                o.TotalAmount,
                o.DiscountAmount)).ToList(),
            subtotal,
            discount,
            subtotal - discount);
    }

    private async Task<PromoCode> ValidatePromoCodeAsync(string code, decimal subtotal, Guid userId)
    {
        var normalized = code.ToUpperInvariant();
        var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code.ToUpper() == normalized);
        var now = DateTime.UtcNow;

        if (promo is null)
            throw new InvalidOperationException($"Promo code \"{code}\" was not found.");
        if (!promo.IsActive)
            throw new InvalidOperationException($"Promo code \"{code}\" is not active.");
        if (now < promo.StartsAt)
            throw new InvalidOperationException($"Promo code \"{code}\" is not active yet.");
        if (now > promo.ExpiresAt)
            throw new InvalidOperationException($"Promo code \"{code}\" has expired.");
        if (promo.MaxActivations > 0 && promo.UsedActivationsCount >= promo.MaxActivations)
            throw new InvalidOperationException($"Promo code \"{code}\" has been fully used.");
        if (promo.MinOrderAmount.HasValue && subtotal < promo.MinOrderAmount.Value)
            throw new InvalidOperationException(
                $"Promo code \"{code}\" requires an order of at least ${promo.MinOrderAmount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}.");

        if (promo.LimitPerUser > 0)
        {
            // A checkout creates one order per seller with the same CreatedAt: count checkouts, not orders
            var usages = await _context.Orders
                .Where(o => o.PromoCodeId == promo.Id && o.UserId == userId)
                .Select(o => o.CreatedAt)
                .Distinct()
                .CountAsync();

            if (usages >= promo.LimitPerUser)
                throw new InvalidOperationException($"You have already used promo code \"{code}\".");
        }

        return promo;
    }

    private static decimal CalculateDiscount(PromoCode promo, decimal subtotal)
    {
        var discount = promo.DiscountType == DiscountType.Percentage
            ? subtotal * promo.DiscountValue / 100
            : promo.DiscountValue;

        if (promo.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, promo.MaxDiscountAmount.Value);

        return Math.Round(Math.Clamp(discount, 0m, subtotal), 2, MidpointRounding.AwayFromZero);
    }

    public async Task UpdateStatusAsync(
        Guid id,
        OrderStatus status,
        Guid? requesterUserId = null,
        bool isAdmin = false)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
            return;

        // A seller may only manage orders that belong to their own store.
        if (!isAdmin && requesterUserId.HasValue)
        {
            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == requesterUserId.Value);

            if (seller == null || order.SellerId != seller.Id)
                throw new UnauthorizedAccessException("Order belongs to another seller");
        }

        // Terminal states must not be changed by sellers.
        if (!isAdmin && order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
            throw new InvalidOperationException("A cancelled or refunded order cannot change its status");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}