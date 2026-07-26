using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductGalleriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductGalleriesController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var images = await _context.ProductGalleries
            .Where(g => g.ProductId == productId)
            .OrderBy(g => g.SortOrder)
            .ToListAsync();

        return Ok(images);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductGallery gallery)
    {
        gallery.Id = Guid.NewGuid();

        _context.ProductGalleries.Add(gallery);
        await _context.SaveChangesAsync();

        return Created($"/api/productgalleries/byproduct/{gallery.ProductId}", gallery);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, ProductGallery updated)
    {
        var gallery = await _context.ProductGalleries.FindAsync(id);
        if (gallery == null) return NotFound();

        gallery.Path = updated.Path;
        gallery.SortOrder = updated.SortOrder;
        gallery.IsMain = updated.IsMain;

        if (updated.IsMain)
            await _context.ResetOtherMainImagesAsync(gallery.ProductId, id);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var gallery = await _context.ProductGalleries.FindAsync(id);
        if (gallery == null) return NotFound();

        _context.ProductGalleries.Remove(gallery);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
