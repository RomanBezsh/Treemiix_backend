using CloneAmazonBack.Data;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserResponse>> GetAllAsync(bool includeInactive)
    {
        var query = _context.Users
            .Include(u => u.UserRole)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        return await query
            .Select(u => new UserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.IsActive, u.UserRole.Name))
            .ToListAsync();
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.UserRole)
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.IsActive, u.UserRole.Name))
            .FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return;

        user.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task HardDeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task ReactivateAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return;

        user.IsActive = true;
        await _context.SaveChangesAsync();
    }
}