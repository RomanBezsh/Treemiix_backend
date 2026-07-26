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
public class ProductQuestionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductQuestionsController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var questions = await _context.ProductQuestions
            .Include(q => q.User)
            .Include(q => q.Answers)
            .Where(q => q.ProductId == productId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return Ok(questions);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var question = await _context.ProductQuestions
            .Include(q => q.User)
            .Include(q => q.Answers)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return NotFound();
        return Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateQuestionRequest request)
    {
        var userId = User.GetUserId();
        var question = new ProductQuestion
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            UserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductQuestions.Add(question);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var question = await _context.ProductQuestions.FindAsync(id);
        if (question == null) return NotFound();

        _context.ProductQuestions.Remove(question);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
