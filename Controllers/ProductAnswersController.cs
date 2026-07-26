using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductAnswersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductAnswersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("byquestion/{questionId}")]
    public async Task<IActionResult> GetByQuestion(Guid questionId)
    {
        var answers = await _context.ProductAnswers
            .Include(a => a.User)
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.IsOfficialAnswer)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(answers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAnswerRequest request)
    {
        var answer = new ProductAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = request.QuestionId,
            UserId = request.UserId,
            Content = request.Content,
            IsOfficialAnswer = request.IsOfficialAnswer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductAnswers.Add(answer);
        await _context.SaveChangesAsync();

        return Created($"/api/productanswers/byquestion/{answer.QuestionId}", answer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, string content)
    {
        var answer = await _context.ProductAnswers.FindAsync(id);
        if (answer == null) return NotFound();

        answer.Content = content;
        answer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var answer = await _context.ProductAnswers.FindAsync(id);
        if (answer == null) return NotFound();

        _context.ProductAnswers.Remove(answer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
