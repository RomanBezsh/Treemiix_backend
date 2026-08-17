using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class PromoCodeService : IPromoCodeService
{
    private readonly AppDbContext _context;

    public PromoCodeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromoCode>> GetAllAsync()
    {
        return await _context.PromoCodes.ToListAsync();
    }

    public async Task<PromoCode?> GetByIdAsync(Guid id)
    {
        return await _context.PromoCodes.FindAsync(id);
    }

    public async Task<PromoCode?> GetByCodeAsync(string code)
    {
        return await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<PromoCode> CreateAsync(CreatePromoCodeRequest request)
    {
        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            DiscountValue = request.DiscountValue,
            DiscountType = request.DiscountType,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MaxActivations = request.MaxActivations,
            LimitPerUser = request.LimitPerUser,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt,
            IsActive = true
        };

        _context.PromoCodes.Add(promo);
        await _context.SaveChangesAsync();

        return promo;
    }

    public async Task UpdateAsync(Guid id, CreatePromoCodeRequest request)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null)
            return;

        promo.DiscountValue = request.DiscountValue;
        promo.DiscountType = request.DiscountType;
        promo.MinOrderAmount = request.MinOrderAmount;
        promo.MaxDiscountAmount = request.MaxDiscountAmount;
        promo.MaxActivations = request.MaxActivations;
        promo.LimitPerUser = request.LimitPerUser;
        promo.StartsAt = request.StartsAt;
        promo.ExpiresAt = request.ExpiresAt;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null)
            return;

        _context.PromoCodes.Remove(promo);
        await _context.SaveChangesAsync();
    }
}