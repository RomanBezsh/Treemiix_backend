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

    public async Task<List<CartItem>> GetByCartAsync(Guid cartId, Guid userId)
    {
        return await _context.CartItems
            .Include(i => i.Product)
            .ThenInclude(p => p.Galleries)
            .Where(i => i.CartId == cartId && i.Cart.UserId == userId)
            .ToListAsync();
    }

    public async Task<CartItem> CreateAsync(Guid userId, CreateCartItemRequest request)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.Id == request.CartId && c.UserId == userId);

        if (cart == null)
            throw new UnauthorizedAccessException("Cart not found or belongs to another user");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);

        if (product == null)
            throw new InvalidOperationException("Product not found or inactive");

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(i =>
                i.CartId == request.CartId &&
                i.ProductId == request.ProductId);

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > product.Stock)
                throw new InvalidOperationException("Not enough stock for the requested quantity");

            existingItem.Quantity = newQuantity;
            await _context.SaveChangesAsync();
            return existingItem;
        }

        if (request.Quantity > product.Stock)
            throw new InvalidOperationException("Not enough stock for the requested quantity");

        var item = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = request.CartId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Price = product.Price
        };

        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<bool> UpdateQuantityAsync(Guid id, int quantity, Guid userId)
    {
        if (quantity < 1)
            throw new InvalidOperationException("Quantity must be at least 1");

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id && i.Cart.UserId == userId);

        if (item == null)
            return false;

        if (quantity > item.Product.Stock)
            throw new InvalidOperationException("Not enough stock for the requested quantity");

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == id && i.Cart.UserId == userId);

        if (item == null)
            return false;

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }
}