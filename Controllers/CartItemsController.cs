using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartItemsController : ControllerBase
{
    private readonly ICartItemService _cartItemService;

    public CartItemsController(ICartItemService cartItemService)
    {
        _cartItemService = cartItemService;
    }

    [HttpGet("bycart/{cartId}")]
    public async Task<IActionResult> GetByCart(Guid cartId)
    {
        var items = await _cartItemService.GetByCartAsync(cartId);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCartItemRequest request)
    {
        var item = await _cartItemService.CreateAsync(request);
        return Created($"/api/cartitems/bycart/{item.CartId}", item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
    {
        var updated = await _cartItemService.UpdateQuantityAsync(id, quantity);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _cartItemService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}