using CloneAmazonBack.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryPriceProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public HistoryPriceProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var history = await _context.HistoryPriceProducts
            .Where(h => h.ProductId == productId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return Ok(history);
    }
}
