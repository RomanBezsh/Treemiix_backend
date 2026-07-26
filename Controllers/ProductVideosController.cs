using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductVideosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductVideosController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var videos = await _context.ProductVideos
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();

        return Ok(videos);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductVideo video)
    {
        video.Id = Guid.NewGuid();

        _context.ProductVideos.Add(video);
        await _context.SaveChangesAsync();

        return Created($"/api/productvideos/byproduct/{video.ProductId}", video);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, ProductVideo updated)
    {
        var video = await _context.ProductVideos.FindAsync(id);
        if (video == null) return NotFound();

        video.Path = updated.Path;
        video.SortOrder = updated.SortOrder;
        video.IsMain = updated.IsMain;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var video = await _context.ProductVideos.FindAsync(id);
        if (video == null) return NotFound();

        _context.ProductVideos.Remove(video);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
