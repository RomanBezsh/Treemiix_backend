using CloneAmazonBack.Extensions;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderItemsController : ControllerBase
{
    private readonly IOrderItemService _orderItemService;

    public OrderItemsController(IOrderItemService orderItemService)
    {
        _orderItemService = orderItemService;
    }

    [HttpGet("byorder/{orderId}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var userId = User.GetUserId();
        var items = await _orderItemService.GetByOrderAsync(orderId, userId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.GetUserId();
        var item = await _orderItemService.GetByIdAsync(id, userId);
        if (item == null) return NotFound();
        return Ok(item);
    }
}