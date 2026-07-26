using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserRolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserRolesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _context.UserRoles.ToListAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = await _context.UserRoles.FindAsync(id);

        if (role == null)
            return NotFound();

        return Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserRole role)
    {
        role.Id = Guid.NewGuid();

        _context.UserRoles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UserRole updated)
    {
        var role = await _context.UserRoles.FindAsync(id);

        if (role == null)
            return NotFound();

        role.Name = updated.Name;
        role.Rights = updated.Rights;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var role = await _context.UserRoles.FindAsync(id);

        if (role == null)
            return NotFound();

        _context.UserRoles.Remove(role);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
