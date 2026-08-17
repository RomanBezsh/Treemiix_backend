using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetMyCartAsync(Guid userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .Include(c => c.PromoCode)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .Include(c => c.PromoCode)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<Cart> CreateAsync(Guid userId)
    {
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return cart;
    }

    public async Task<bool> UserHasCartAsync(Guid userId)
    {
        return await _context.Carts.AnyAsync(c => c.UserId == userId);
    }

    public async Task ApplyPromoCodeAsync(Guid id, Guid? promoCodeId, Guid userId)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cart == null)
            return;

        cart.PromoCodeId = promoCodeId;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cart == null)
            return;

        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();
    }
}