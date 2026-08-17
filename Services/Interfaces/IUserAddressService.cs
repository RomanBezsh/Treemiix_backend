using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IUserAddressService
{
    Task<List<UserAddress>> GetByUserAsync(Guid userId);
    Task<UserAddress?> GetByIdAsync(Guid id);
    Task<UserAddress> CreateAsync(Guid userId, CreateAddressRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateAddressRequest request);
    Task<bool> DeleteAsync(Guid id);
}