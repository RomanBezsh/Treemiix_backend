using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class GiftCardService : IGiftCardService
{
    private readonly AppDbContext _context;

    public GiftCardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GiftCard>> GetAllAsync(Guid purchasedByUserId)
    {
        return await _context.GiftCards
            .Where(g => g.PurchasedByUserId == purchasedByUserId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<GiftCard?> GetByIdAsync(Guid id)
    {
        return await _context.GiftCards.FindAsync(id);
    }

    public async Task<GiftCard?> GetByCodeAsync(string code)
    {
        return await _context.GiftCards.FirstOrDefaultAsync(g => g.Code == code);
    }

    public async Task<GiftCard> CreateAsync(Guid userId, CreateGiftCardRequest request)
    {
        var card = new GiftCard
        {
            Id = Guid.NewGuid(),
            Code = AppDbContextExtensions.GenerateGiftCardCode(),
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            PurchasedByUserId = userId,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.GiftCards.Add(card);
        await _context.SaveChangesAsync();

        return card;
    }

    public async Task ActivateAsync(Guid id, Guid userId)
    {
        var card = await _context.GiftCards.FindAsync(id);
        if (card == null)
            return;

        if (!card.IsActive || card.CurrentBalance <= 0)
            throw new InvalidOperationException("Gift card is not active or has no balance");

        if (card.ActivatedByUserId.HasValue)
            throw new InvalidOperationException("Gift card already activated");

        card.ActivatedByUserId = userId;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var card = await _context.GiftCards.FindAsync(id);
        if (card == null)
            return;

        card.IsActive = false;
        await _context.SaveChangesAsync();
    }
}