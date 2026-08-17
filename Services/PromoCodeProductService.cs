using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class PromoCodeProductService : IPromoCodeProductService
{
    private readonly AppDbContext _context;

    public PromoCodeProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromoCodeProduct>> GetByPromoCodeAsync(Guid promoCodeId)
    {
        return await _context.PromoCodeProducts
            .Include(pp => pp.Product)
            .Where(pp => pp.PromoCodeId == promoCodeId)
            .ToListAsync();
    }

    public async Task<PromoCodeProduct> CreateAsync(CreatePromoCodeProductRequest request)
    {
        var link = new PromoCodeProduct
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            PromoCodeId = request.PromoCodeId
        };

        _context.PromoCodeProducts.Add(link);
        await _context.SaveChangesAsync();

        return link;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var link = await _context.PromoCodeProducts.FindAsync(id);
        if (link == null)
            return false;

        _context.PromoCodeProducts.Remove(link);
        await _context.SaveChangesAsync();

        return true;
    }
}