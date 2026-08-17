using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface ICartItemService
{
    Task<List<CartItem>> GetByCartAsync(Guid cartId);
    Task<CartItem> CreateAsync(CreateCartItemRequest request);
    Task<bool> UpdateQuantityAsync(Guid id, int quantity);
    Task<bool> DeleteAsync(Guid id);
}