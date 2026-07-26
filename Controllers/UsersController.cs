using CloneAmazonBack.Data;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _context.Users
            .Include(u => u.UserRole)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        var users = await query
            .Select(u => new UserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.IsActive, u.UserRole.Name))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.IsActive, u.UserRole.Name))
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.IsActive = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
