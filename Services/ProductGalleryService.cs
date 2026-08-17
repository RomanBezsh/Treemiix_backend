using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductGalleryService : IProductGalleryService
{
    private readonly AppDbContext _context;

    public ProductGalleryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductGallery>> GetByProductAsync(Guid productId)
    {
        return await _context.ProductGalleries
            .Where(g => g.ProductId == productId)
            .OrderBy(g => g.SortOrder)
            .ToListAsync();
    }

    public async Task<ProductGallery> CreateAsync(CreateGalleryRequest request)
    {
        var gallery = new ProductGallery
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Path = request.Path,
            SortOrder = request.SortOrder,
            IsMain = request.IsMain
        };

        _context.ProductGalleries.Add(gallery);
        await _context.SaveChangesAsync();

        return gallery;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateGalleryRequest request)
    {
        var gallery = await _context.ProductGalleries.FindAsync(id);
        if (gallery == null)
            return false;

        gallery.Path = request.Path;
        gallery.SortOrder = request.SortOrder;
        gallery.IsMain = request.IsMain;

        if (request.IsMain)
            await _context.ResetOtherMainImagesAsync(gallery.ProductId, id);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var gallery = await _context.ProductGalleries.FindAsync(id);
        if (gallery == null)
            return false;

        _context.ProductGalleries.Remove(gallery);
        await _context.SaveChangesAsync();

        return true;
    }
}