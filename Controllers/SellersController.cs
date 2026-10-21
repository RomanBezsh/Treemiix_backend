using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SellersController : ControllerBase
{
    private readonly ISellerService _sellerService;

    public SellersController(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sellers = await _sellerService.GetAllAsync();
        return Ok(sellers);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var seller = await _sellerService.GetByIdAsync(id);
        if (seller == null) return NotFound();
        return Ok(seller);
    }

    [AllowAnonymous]
    [HttpGet("byuser/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var seller = await _sellerService.GetByUserAsync(userId);
        if (seller == null) return NotFound();
        return Ok(seller);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSellerRequest request)
    {
        var userId = User.GetUserId();

        // Sellers cannot assign their own commission or skip verification.
        if (!User.IsInRole(Roles.Admin))
            request = request with { CommissionRate = 0.10m };

        try
        {
            var seller = await _sellerService.CreateAsync(userId, request);
            return CreatedAtAction(nameof(GetById), new { id = seller.Id }, seller);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateSellerRequest request)
    {
        var existing = await _sellerService.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (!User.IsInRole(Roles.Admin))
        {
            if (existing.UserId != User.GetUserId())
                return Forbid();

            // Commission/status are managed by the platform, not by the seller.
            request = request with { CommissionRate = existing.CommissionRate };
        }

        try
        {
            await _sellerService.UpdateAsync(id, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, SellerStatus status)
    {
        await _sellerService.UpdateStatusAsync(id, status);
        return NoContent();
    }
}