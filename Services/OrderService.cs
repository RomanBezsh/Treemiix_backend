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

        var discount = await _context.ApplyPromoCodeAsync(request.PromoCodeId, totalAmount);
        if (discount != null)
            order.DiscountAmount = discount.DiscountAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task UpdateStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}