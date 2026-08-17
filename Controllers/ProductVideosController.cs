using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
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
    public async Task<IActionResult> Create(CreateVideoRequest request)
    {
        var video = new ProductVideo
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Path = request.Path,
            SortOrder = request.SortOrder,
            IsMain = request.IsMain
        };

        _context.ProductVideos.Add(video);
        await _context.SaveChangesAsync();

        return Created($"/api/productvideos/byproduct/{video.ProductId}", video);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateVideoRequest request)
    {
        var video = await _context.ProductVideos.FindAsync(id);
        if (video == null) return NotFound();

        video.Path = request.Path;
        video.SortOrder = request.SortOrder;
        video.IsMain = request.IsMain;

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
