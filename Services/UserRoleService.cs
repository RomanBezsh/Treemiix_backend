using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class UserRoleService : IUserRoleService
{
    private readonly AppDbContext _context;

    public UserRoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserRole>> GetAllAsync()
    {
        return await _context.UserRoles.ToListAsync();
    }

    public async Task<UserRole?> GetByIdAsync(Guid id)
    {
        return await _context.UserRoles.FindAsync(id);
    }

    public async Task<UserRole> CreateAsync(CreateRoleRequest request)
    {
        var role = new UserRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Rights = request.Rights
        };

        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        return role;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateRoleRequest request)
    {
        var role = await _context.UserRoles.FindAsync(id);
        if (role == null)
            return false;

        role.Name = request.Name;
        role.Rights = request.Rights;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var role = await _context.UserRoles.FindAsync(id);
        if (role == null)
            return false;

        _context.UserRoles.Remove(role);
        await _context.SaveChangesAsync();

        return true;
    }
}