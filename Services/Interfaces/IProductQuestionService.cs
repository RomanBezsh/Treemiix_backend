using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductQuestionService
{
    Task<List<ProductQuestion>> GetByProductAsync(Guid productId);
    Task<ProductQuestion?> GetByIdAsync(Guid id);
    Task<ProductQuestion> CreateAsync(Guid userId, CreateQuestionRequest request);
    Task<bool> DeleteAsync(Guid id);
}