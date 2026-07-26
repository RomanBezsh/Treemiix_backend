using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfilesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserProfilesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        return Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, UpdateProfileRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return NotFound("User not found");

        var existingProfile = await _context.UserProfiles.FindAsync(userId);
        if (existingProfile != null)
            return Conflict("Profile already exists");

        var profile = new UserProfile
        {
            UserId = userId,
            DateOfBirth = request.DateOfBirth,
            AvatarUrl = request.AvatarUrl
        };

        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByUser), new { userId }, profile);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(Guid userId, UpdateProfileRequest request)
    {
        var profile = await _context.UserProfiles.FindAsync(userId);

        if (profile == null)
            return NotFound();

        profile.DateOfBirth = request.DateOfBirth;
        profile.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
