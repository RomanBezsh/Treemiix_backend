using CloneAmazonBack.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderItemsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("byorder/{orderId}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var items = await _context.OrderItems
            .Include(i => i.Product)
            .Where(i => i.OrderId == orderId)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _context.OrderItems
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null) return NotFound();
        return Ok(item);
    }
}
