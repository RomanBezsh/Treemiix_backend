using CloneAmazonBack.Extensions;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCart()
    {
        var userId = User.GetUserId();
        var cart = await _cartService.GetMyCartAsync(userId);

        if (cart == null)
            return NotFound();

        return Ok(cart);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var cart = await _cartService.GetByIdAsync(id);

        if (cart == null) return NotFound();
        return Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var userId = User.GetUserId();
        var existingCart = await _cartService.UserHasCartAsync(userId);
        if (existingCart)
            return Conflict("User already has a cart");

        var cart = await _cartService.CreateAsync(userId);

        return CreatedAtAction(nameof(GetById), new { id = cart.Id }, cart);
    }

    [HttpPatch("{id}/promocode")]
    public async Task<IActionResult> ApplyPromoCode(Guid id, Guid? promoCodeId)
    {
        await _cartService.ApplyPromoCodeAsync(id, promoCodeId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _cartService.DeleteAsync(id);
        return NoContent();
    }
}