using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductReviewsController : ControllerBase
{
    private readonly IProductReviewService _reviewService;

    public ProductReviewsController(IProductReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var reviews = await _reviewService.GetByProductAsync(productId);
        return Ok(reviews);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var review = await _reviewService.GetByIdAsync(id);
        if (review == null) return NotFound();
        return Ok(review);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewRequest request)
    {
        var userId = User.GetUserId();
        var review = await _reviewService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateReviewRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);
        var updated = await _reviewService.UpdateAsync(id, request, userId, isAdmin);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);
        var deleted = await _reviewService.DeleteAsync(id, userId, isAdmin);
        if (!deleted) return NotFound();
        return NoContent();
    }
}