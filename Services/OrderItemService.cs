using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class OrderItemService : IOrderItemService
{
    private readonly AppDbContext _context;

    public OrderItemService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderItem>> GetByOrderAsync(Guid orderId, Guid userId)
    {
        return await _context.OrderItems
            .Include(i => i.Product)
            .Where(i => i.OrderId == orderId && i.Order.UserId == userId)
            .ToListAsync();
    }

    public async Task<OrderItem?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.OrderItems
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id && i.Order.UserId == userId);
    }
}