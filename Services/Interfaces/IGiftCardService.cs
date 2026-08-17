using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IGiftCardService
{
    Task<List<GiftCard>> GetAllAsync(Guid purchasedByUserId);
    Task<GiftCard?> GetByIdAsync(Guid id);
    Task<GiftCard?> GetByCodeAsync(string code);
    Task<GiftCard> CreateAsync(Guid userId, CreateGiftCardRequest request);
    Task ActivateAsync(Guid id, Guid userId);
    Task DeleteAsync(Guid id);
}