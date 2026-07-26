using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? sellerId)
    {
        var userId = User.GetUserId();
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .WhereIf(sellerId, o => o.SellerId == sellerId!.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .Include(o => o.PromoCode)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var userId = User.GetUserId();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SellerId = request.SellerId,
            PromoCodeId = request.PromoCodeId,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var totalAmount = 0m;
        foreach (var itemRequest in request.Items)
        {
            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = itemRequest.ProductId,
                ProductName = itemRequest.ProductName,
                ProductPrice = itemRequest.ProductPrice,
                ProductAvatarUrl = itemRequest.ProductAvatarUrl,
                Quantity = itemRequest.Quantity,
                TotalPrice = itemRequest.ProductPrice * itemRequest.Quantity
            };

            totalAmount += item.TotalPrice;
            order.Items.Add(item);
        }

        order.TotalAmount = totalAmount;

        var discount = await _context.ApplyPromoCodeAsync(request.PromoCodeId, totalAmount);
        if (discount != null)
            order.DiscountAmount = discount.DiscountAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
