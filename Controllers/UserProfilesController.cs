using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly AppDbContext _context;

    public UserProfilesController(IUserProfileService profileService, AppDbContext context)
    {
        _profileService = profileService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.GetUserId();
        var profile = await _profileService.GetByUserAsync(userId);

        if (profile == null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");
            
            return Ok(new { 
                user = new { 
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email
                }, 
                dateOfBirth = (DateTime?)null, 
                avatarUrl = (string?)null 
            });
        }

        return Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();

        try
        {
            var profile = await _profileService.CreateAsync(userId, request);
            return CreatedAtAction(nameof(GetMyProfile), null, profile);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        try
        {
            var updated = await _profileService.UpdateAsync(userId, request);

            if (!updated)
                return NotFound("Profile not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            // Возвращаем детали ошибки для отладки
            return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}