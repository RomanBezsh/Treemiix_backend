using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class ProductReviewService : IProductReviewService
{
    private readonly AppDbContext _context;

    public ProductReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductReview>> GetByProductAsync(Guid productId)
    {
        return await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProductReview?> GetByIdAsync(Guid id)
    {
        return await _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Gallery)
            .Include(r => r.Video)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ProductReview> CreateAsync(Guid userId, CreateReviewRequest request)
    {
        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = request.ProductId,
            ProductGalleryId = request.ProductGalleryId,
            ProductVideoId = request.ProductVideoId,
            Text = request.Text,
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        return review;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateReviewRequest request)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null)
            return false;

        review.Text = request.Text;
        review.Rating = request.Rating;
        review.ProductGalleryId = request.ProductGalleryId;
        review.ProductVideoId = request.ProductVideoId;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null)
            return false;

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();

        return true;
    }
}