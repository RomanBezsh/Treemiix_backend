using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfilesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserProfilesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.GetUserId();
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        return Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();

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

        return CreatedAtAction(nameof(GetMyProfile), null, profile);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var profile = await _context.UserProfiles.FindAsync(userId);

        if (profile == null)
            return NotFound();

        profile.DateOfBirth = request.DateOfBirth;
        profile.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
