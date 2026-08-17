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
        var items = await _orderItemService.GetByOrderAsync(orderId);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _orderItemService.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }
}