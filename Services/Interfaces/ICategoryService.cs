using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category> CreateAsync(CreateCategoryRequest request);
    Task UpdateAsync(Guid id, CreateCategoryRequest request);
    Task DeleteAsync(Guid id);
}