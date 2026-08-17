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

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateGalleryRequest request)
    {
        var gallery = new ProductGallery
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Path = request.Path,
            SortOrder = request.SortOrder,
            IsMain = request.IsMain
        };

        _context.ProductGalleries.Add(gallery);
        await _context.SaveChangesAsync();

        return Created($"/api/productgalleries/byproduct/{gallery.ProductId}", gallery);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateGalleryRequest request)
    {
        var gallery = await _context.ProductGalleries.FindAsync(id);
        if (gallery == null) return NotFound();

        gallery.Path = request.Path;
        gallery.SortOrder = request.SortOrder;
        gallery.IsMain = request.IsMain;

        if (request.IsMain)
            await _context.ResetOtherMainImagesAsync(gallery.ProductId, id);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
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
