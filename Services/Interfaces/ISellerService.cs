using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface ISellerService
{
    Task<List<Seller>> GetAllAsync();
    Task<List<AdminSellerResponse>> GetAllForAdminAsync(SellerStatus? status);
    Task<Seller?> GetByIdAsync(Guid id);
    Task<AdminSellerResponse?> GetAdminByIdAsync(Guid id);
    Task<Seller?> GetByUserAsync(Guid userId);

    Task<Seller> StartOnboardingAsync(Guid userId);
    Task<Seller> UpdateBusinessAsync(Guid userId, UpdateSellerBusinessRequest request);
    Task<Seller> UpdateStorefrontAsync(Guid userId, UpdateSellerStorefrontRequest request);
    Task<Seller> UpdatePayoutAsync(Guid userId, UpdateSellerPayoutRequest request);
    Task<Seller> SubmitAsync(Guid userId);

    Task UpdateAsync(Guid id, UpdateSellerStorefrontRequest request);

    Task<Seller> ApproveAsync(Guid id);
    Task<Seller> RejectAsync(Guid id, string reason);
    Task<Seller> SuspendAsync(Guid id);
    Task<Seller> ReactivateAsync(Guid id);
    Task<Seller> MarkSubscriptionPaidAsync(Guid id, DateTime? paidUntil);
}
