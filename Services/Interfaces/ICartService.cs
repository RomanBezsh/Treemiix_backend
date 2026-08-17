using CloneAmazonBack.Models;

namespace CloneAmazonBack.Services.Interfaces;

public interface ICartService
{
    Task<Cart?> GetMyCartAsync(Guid userId);
    Task<Cart?> GetByIdAsync(Guid id, Guid userId);
    Task<bool> UserHasCartAsync(Guid userId);
    Task<Cart> CreateAsync(Guid userId);
    Task ApplyPromoCodeAsync(Guid id, Guid? promoCodeId, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}