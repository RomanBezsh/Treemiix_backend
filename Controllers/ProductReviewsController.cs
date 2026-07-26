using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductReviewsController(AppDbContext context)
    {
        _context = context;
    }

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
    public async Task<IActionResult> Create(ProductReview review)
    {
        review.Id = Guid.NewGuid();
        review.CreatedAt = DateTime.UtcNow;

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, ProductReview updated)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();

        review.Text = updated.Text;
        review.Rating = updated.Rating;
        review.ProductGalleryId = updated.ProductGalleryId;
        review.ProductVideoId = updated.ProductVideoId;

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
