using CloneAmazonBack.Extensions;
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
        var userId = User.GetUserId();
        var items = await _cartItemService.GetByCartAsync(cartId, userId);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCartItemRequest request)
    {
        var userId = User.GetUserId();
        try
        {
            var item = await _cartItemService.CreateAsync(userId, request);
            return Created($"/api/cartitems/bycart/{item.CartId}", item);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(Guid id, int quantity)
    {
        var userId = User.GetUserId();
        var updated = await _cartItemService.UpdateQuantityAsync(id, quantity, userId);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var deleted = await _cartItemService.DeleteAsync(id, userId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}