using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserAddressesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserAddressesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("byuser/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var addresses = await _context.UserAddresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return Ok(addresses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var address = await _context.UserAddresses.FindAsync(id);

        if (address == null)
            return NotFound();

        return Ok(address);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAddressRequest request)
    {
        if (request.IsDefault)
        {
            var currentDefault = await _context.UserAddresses
                .Where(a => a.UserId == request.UserId && a.IsDefault)
                .FirstOrDefaultAsync();

            if (currentDefault != null)
                currentDefault.IsDefault = false;
        }

        var address = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Country = request.Country,
            City = request.City,
            Street = request.Street,
            Building = request.Building,
            Apartment = request.Apartment,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault
        };

        _context.UserAddresses.Add(address);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = address.Id }, address);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateAddressRequest request)
    {
        var address = await _context.UserAddresses.FindAsync(id);

        if (address == null)
            return NotFound();

        if (request.IsDefault && !address.IsDefault)
        {
            var currentDefault = await _context.UserAddresses
                .Where(a => a.UserId == address.UserId && a.IsDefault && a.Id != id)
                .FirstOrDefaultAsync();

            if (currentDefault != null)
                currentDefault.IsDefault = false;
        }

        address.Country = request.Country;
        address.City = request.City;
        address.Street = request.Street;
        address.Building = request.Building;
        address.Apartment = request.Apartment;
        address.PostalCode = request.PostalCode;
        address.IsDefault = request.IsDefault;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var address = await _context.UserAddresses.FindAsync(id);

        if (address == null)
            return NotFound();

        _context.UserAddresses.Remove(address);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
