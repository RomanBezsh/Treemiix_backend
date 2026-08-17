using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface ISellerService
{
    Task<List<Seller>> GetAllAsync();
    Task<Seller?> GetByIdAsync(Guid id);
    Task<Seller?> GetByUserAsync(Guid userId);
    Task<Seller> CreateAsync(Guid userId, CreateSellerRequest request);
    Task UpdateAsync(Guid id, CreateSellerRequest request);
    Task UpdateStatusAsync(Guid id, SellerStatus status);
}