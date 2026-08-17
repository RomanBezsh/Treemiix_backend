using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductVideoService : IProductVideoService
{
    private readonly AppDbContext _context;

    public ProductVideoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductVideo>> GetByProductAsync(Guid productId)
    {
        return await _context.ProductVideos
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();
    }

    public async Task<ProductVideo> CreateAsync(CreateVideoRequest request)
    {
        var video = new ProductVideo
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Path = request.Path,
            SortOrder = request.SortOrder,
            IsMain = request.IsMain
        };

        _context.ProductVideos.Add(video);
        await _context.SaveChangesAsync();

        return video;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateVideoRequest request)
    {
        var video = await _context.ProductVideos.FindAsync(id);
        if (video == null)
            return false;

        video.Path = request.Path;
        video.SortOrder = request.SortOrder;
        video.IsMain = request.IsMain;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var video = await _context.ProductVideos.FindAsync(id);
        if (video == null)
            return false;

        _context.ProductVideos.Remove(video);
        await _context.SaveChangesAsync();

        return true;
    }
}