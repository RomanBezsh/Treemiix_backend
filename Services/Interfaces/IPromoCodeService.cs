using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IPromoCodeService
{
    Task<List<PromoCode>> GetAllAsync();
    Task<PromoCode?> GetByIdAsync(Guid id);
    Task<PromoCode?> GetByCodeAsync(string code);
    Task<PromoCode> CreateAsync(CreatePromoCodeRequest request);
    Task UpdateAsync(Guid id, CreatePromoCodeRequest request);
    Task DeleteAsync(Guid id);
}