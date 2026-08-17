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

    public UserProfilesController(IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.GetUserId();
        var profile = await _profileService.GetByUserAsync(userId);

        if (profile == null)
            return NotFound();

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
        var updated = await _profileService.UpdateAsync(userId, request);

        if (!updated)
            return NotFound();

        return NoContent();
    }
}