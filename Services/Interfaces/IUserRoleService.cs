using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;

namespace CloneAmazonBack.Services.Interfaces;

public interface IUserRoleService
{
    Task<List<UserRole>> GetAllAsync();
    Task<UserRole?> GetByIdAsync(Guid id);
    Task<UserRole> CreateAsync(CreateRoleRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateRoleRequest request);
    Task<bool> DeleteAsync(Guid id);
}