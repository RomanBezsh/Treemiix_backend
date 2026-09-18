using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IProductService
{
    Task<(List<Product> Items, int TotalCount)> GetAllAsync(
        Guid? categoryId,
        Guid? sellerId,
        bool? isActive,
        string? search,
        int page,
        int pageSize);

    Task<List<Product>> GetDealsAsync();

    Task<Product?> GetByIdAsync(Guid id);

    Task<Product> CreateAsync(CreateProductRequest request);

    Task UpdateAsync(Guid id, CreateProductRequest request);

    Task SoftDeleteAsync(Guid id);

    Task HardDeleteAsync(Guid id);
}