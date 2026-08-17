using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserAddressesController : ControllerBase
{
    private readonly IUserAddressService _addressService;

    public UserAddressesController(IUserAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet("byuser/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var currentUserId = User.GetUserId();
        if (userId != currentUserId)
            return Forbid();

        var addresses = await _addressService.GetByUserAsync(userId);
        return Ok(addresses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var address = await _addressService.GetByIdAsync(id);
        if (address == null)
            return NotFound();

        return Ok(address);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAddressRequest request)
    {
        var userId = User.GetUserId();
        var address = await _addressService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = address.Id }, address);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateAddressRequest request)
    {
        var updated = await _addressService.UpdateAsync(id, request);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _addressService.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}