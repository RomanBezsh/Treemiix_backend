using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IUserProfileService
{
    Task<UserProfile?> GetByUserAsync(Guid userId);
    Task<UserProfile> CreateAsync(Guid userId, UpdateProfileRequest request);
    Task<bool> UpdateAsync(Guid userId, UpdateProfileRequest request);
}