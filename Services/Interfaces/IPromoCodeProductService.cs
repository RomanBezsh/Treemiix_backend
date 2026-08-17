using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IPromoCodeProductService
{
    Task<List<PromoCodeProduct>> GetByPromoCodeAsync(Guid promoCodeId);
    Task<PromoCodeProduct> CreateAsync(CreatePromoCodeProductRequest request);
    Task<bool> DeleteAsync(Guid id);
}