using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductAttributeValueService
{
    Task<List<ProductAttributeValue>> GetByProductAsync(Guid productId);
    Task<ProductAttributeValue> CreateAsync(CreateAttributeRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateAttributeRequest request);
    Task<bool> DeleteAsync(Guid id);
}