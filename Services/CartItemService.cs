using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class CartItemService : ICartItemService
{
    private readonly AppDbContext _context;

    public CartItemService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CartItem>> GetByCartAsync(Guid cartId)
    {
        return await _context.CartItems
            .Include(i => i.Product)
            .Where(i => i.CartId == cartId)
            .ToListAsync();
    }

    public async Task<CartItem> CreateAsync(CreateCartItemRequest request)
    {
        var item = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = request.CartId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Price = request.Price
        };

        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<bool> UpdateQuantityAsync(Guid id, int quantity)
    {
        var item = await _context.CartItems.FindAsync(id);
        if (item == null)
            return false;

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _context.CartItems.FindAsync(id);
        if (item == null)
            return false;

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }
}