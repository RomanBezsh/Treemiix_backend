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
public class ProductReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var review = await _context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Gallery)
            .Include(r => r.Video)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null) return NotFound();
        return Ok(review);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewRequest request)
    {
        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            UserId = User.GetUserId(),
            ProductId = request.ProductId,
            ProductGalleryId = request.ProductGalleryId,
            ProductVideoId = request.ProductVideoId,
            Text = request.Text,
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateReviewRequest request)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();

        review.Text = request.Text;
        review.Rating = request.Rating;
        review.ProductGalleryId = request.ProductGalleryId;
        review.ProductVideoId = request.ProductVideoId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
