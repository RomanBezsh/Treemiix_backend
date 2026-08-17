using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductVideoService
{
    Task<List<ProductVideo>> GetByProductAsync(Guid productId);
    Task<ProductVideo> CreateAsync(CreateVideoRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateVideoRequest request);
    Task<bool> DeleteAsync(Guid id);
}