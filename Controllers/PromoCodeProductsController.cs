using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromoCodeProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PromoCodeProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bypromocode/{promoCodeId}")]
    public async Task<IActionResult> GetByPromoCode(Guid promoCodeId)
    {
        var items = await _context.PromoCodeProducts
            .Include(pp => pp.Product)
            .Where(pp => pp.PromoCodeId == promoCodeId)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PromoCodeProduct link)
    {
        link.Id = Guid.NewGuid();

        _context.PromoCodeProducts.Add(link);
        await _context.SaveChangesAsync();

        return Created($"/api/promocodeproducts/bypromocode/{link.PromoCodeId}", link);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var link = await _context.PromoCodeProducts.FindAsync(id);
        if (link == null) return NotFound();

        _context.PromoCodeProducts.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
