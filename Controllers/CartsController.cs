using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyCart()
    {
        var userId = User.GetUserId();
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .Include(c => c.PromoCode)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return NotFound();

        return Ok(cart);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cart == null) return NotFound();
        return Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var userId = User.GetUserId();
        var existingCart = await _context.Carts.AnyAsync(c => c.UserId == userId);
        if (existingCart)
            return Conflict("User already has a cart");

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cart.Id }, cart);
    }

    [HttpPatch("{id}/promocode")]
    public async Task<IActionResult> ApplyPromoCode(Guid id, Guid? promoCodeId)
    {
        var cart = await _context.Carts.FindAsync(id);
        if (cart == null) return NotFound();

        cart.PromoCodeId = promoCodeId;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cart = await _context.Carts.FindAsync(id);
        if (cart == null) return NotFound();

        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
