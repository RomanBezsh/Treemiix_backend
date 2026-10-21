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
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? sellerId)
    {
        var userId = User.GetUserId();

        if (User.IsInRole(Roles.Seller) && !User.IsInRole(Roles.Admin))
        {
            var orders = await _orderService.GetForSellerAsync(userId);
            return Ok(orders);
        }

        var userOrders = await _orderService.GetAllAsync(userId, sellerId);
        return Ok(userOrders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.GetUserId();
        var order = await _orderService.GetByIdAsync(id, userId);

        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var userId = User.GetUserId();
        try
        {
            var order = await _orderService.CreateAsync(userId, request);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Seller}")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, OrderStatus status)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        try
        {
            await _orderService.UpdateStatusAsync(id, status, userId, isAdmin);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }
}