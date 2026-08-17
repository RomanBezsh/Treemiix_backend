using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class SellerService : ISellerService
{
    private readonly AppDbContext _context;

    public SellerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seller>> GetAllAsync()
    {
        return await _context.Sellers.ToListAsync();
    }

    public async Task<Seller?> GetByIdAsync(Guid id)
    {
        return await _context.Sellers.FindAsync(id);
    }

    public async Task<Seller?> GetByUserAsync(Guid userId)
    {
        return await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Seller> CreateAsync(Guid userId, CreateSellerRequest request)
    {
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoreName = request.StoreName,
            StoreSlug = request.StoreSlug,
            LogoUrl = request.LogoUrl,
            Description = request.Description,
            TaxNumber = request.TaxNumber,
            LegalAddress = request.LegalAddress,
            BankAccount = request.BankAccount,
            CommissionRate = request.CommissionRate,
            Status = SellerStatus.PendingVerification,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        return seller;
    }

    public async Task UpdateAsync(Guid id, CreateSellerRequest request)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller == null)
            return;

        seller.StoreName = request.StoreName;
        seller.StoreSlug = request.StoreSlug;
        seller.LogoUrl = request.LogoUrl;
        seller.Description = request.Description;
        seller.TaxNumber = request.TaxNumber;
        seller.LegalAddress = request.LegalAddress;
        seller.BankAccount = request.BankAccount;
        seller.CommissionRate = request.CommissionRate;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid id, SellerStatus status)
    {
        var seller = await _context.Sellers.FindAsync(id);
        if (seller == null)
            return;

        seller.Status = status;
        await _context.SaveChangesAsync();
    }
}