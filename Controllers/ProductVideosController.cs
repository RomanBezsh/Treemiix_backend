using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductVideosController : ControllerBase
{
    private readonly IProductVideoService _videoService;

    public ProductVideosController(IProductVideoService videoService)
    {
        _videoService = videoService;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var videos = await _videoService.GetByProductAsync(productId);
        return Ok(videos);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateVideoRequest request)
    {
        var video = await _videoService.CreateAsync(request);
        return Created($"/api/productvideos/byproduct/{video.ProductId}", video);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateVideoRequest request)
    {
        var updated = await _videoService.UpdateAsync(id, request);
        if (!updated) return NotFound();
        return NoContent();
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _videoService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}