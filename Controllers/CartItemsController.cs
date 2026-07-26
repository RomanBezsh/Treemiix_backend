using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartItemsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bycart/{cartId}")]
    public async Task<IActionResult> GetByCart(Guid cartId)
    {
        var items = await _context.CartItems
            .Include(i => i.Product)
            .Where(i => i.CartId == cartId)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCartItemRequest request)
    {
        var item = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = request.CartId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Price = request.Price
        };

        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();

        return Created($"/api/cartitems/bycart/{item.CartId}", item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
    {
        var item = await _context.CartItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.CartItems.FindAsync(id);
        if (item == null) return NotFound();

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
