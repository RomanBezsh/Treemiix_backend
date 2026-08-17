using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductGalleriesController : ControllerBase
{
    private readonly IProductGalleryService _galleryService;

    public ProductGalleriesController(IProductGalleryService galleryService)
    {
        _galleryService = galleryService;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var images = await _galleryService.GetByProductAsync(productId);
        return Ok(images);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateGalleryRequest request)
    {
        var gallery = await _galleryService.CreateAsync(request);
        return Created($"/api/productgalleries/byproduct/{gallery.ProductId}", gallery);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateGalleryRequest request)
    {
        var updated = await _galleryService.UpdateAsync(id, request);
        if (!updated) return NotFound();
        return NoContent();
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _galleryService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}