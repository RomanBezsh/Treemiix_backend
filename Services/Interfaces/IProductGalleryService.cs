using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductGalleryService
{
    Task<List<ProductGallery>> GetByProductAsync(Guid productId);
    Task<ProductGallery> CreateAsync(CreateGalleryRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateGalleryRequest request);
    Task<bool> DeleteAsync(Guid id);
}