using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionVotesController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuestionVotesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Vote(VoteRequest request)
    {
        var existingVote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == request.QuestionId && v.UserId == request.UserId);

        if (existingVote != null)
        {
            existingVote.Value = request.Value;
        }
        else
        {
            _context.QuestionVotes.Add(new QuestionVote
            {
                Id = Guid.NewGuid(),
                QuestionId = request.QuestionId,
                UserId = request.UserId,
                Value = request.Value
            });
        }

        await _context.RecalculateVotesCountAsync(request.QuestionId);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveVote(Guid id)
    {
        var vote = await _context.QuestionVotes.FindAsync(id);
        if (vote == null) return NotFound();

        var questionId = vote.QuestionId;

        _context.QuestionVotes.Remove(vote);
        await _context.RecalculateVotesCountAsync(questionId);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
