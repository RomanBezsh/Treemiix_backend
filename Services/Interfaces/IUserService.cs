using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync(bool includeInactive);
    Task<UserResponse?> GetByIdAsync(Guid id);
    Task UpdateAsync(Guid id, UpdateUserRequest request);
    Task SoftDeleteAsync(Guid id);
    Task HardDeleteAsync(Guid id);
    Task ReactivateAsync(Guid id);
}