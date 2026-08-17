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

    public async Task<List<OrderItem>> GetByOrderAsync(Guid orderId)
    {
        return await _context.OrderItems
            .Include(i => i.Product)
            .Where(i => i.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<OrderItem?> GetByIdAsync(Guid id)
    {
        return await _context.OrderItems
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}