using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductReviewService
{
    Task<List<ProductReview>> GetByProductAsync(Guid productId);
    Task<ProductReview?> GetByIdAsync(Guid id);
    Task<ProductReview> CreateAsync(Guid userId, CreateReviewRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateReviewRequest request);
    Task<bool> DeleteAsync(Guid id);
}