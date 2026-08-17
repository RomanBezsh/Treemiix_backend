using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface ICartItemService
{
    Task<List<CartItem>> GetByCartAsync(Guid cartId, Guid userId);
    Task<CartItem> CreateAsync(Guid userId, CreateCartItemRequest request);
    Task<bool> UpdateQuantityAsync(Guid id, int quantity, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
}