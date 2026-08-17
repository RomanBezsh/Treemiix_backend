using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var users = await _userService.GetAllAsync(includeInactive);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request)
    {
        await _userService.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        await _userService.SoftDeleteAsync(id);
        return NoContent();
    }

    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(Guid id)
    {
        await _userService.HardDeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        await _userService.ReactivateAsync(id);
        return NoContent();
    }
}