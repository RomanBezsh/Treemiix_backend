using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _context;

    public UserProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserAsync(Guid userId)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<UserProfile> CreateAsync(Guid userId, UpdateProfileRequest request)
    {
        var existingProfile = await _context.UserProfiles.FindAsync(userId);
        if (existingProfile != null)
            throw new InvalidOperationException("Profile already exists");

        var profile = new UserProfile
        {
            UserId = userId,
            DateOfBirth = request.DateOfBirth,
            AvatarUrl = request.AvatarUrl
        };

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile;
    }

    public async Task<bool> UpdateAsync(Guid userId, UpdateProfileRequest request)
    {
        var profile = await _context.UserProfiles.FindAsync(userId);
        if (profile == null)
            return false;

        profile.DateOfBirth = request.DateOfBirth;
        profile.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync();

        return true;
    }
}