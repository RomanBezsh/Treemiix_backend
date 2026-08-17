using CloneAmazonBack.Data;
using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Services;

public class QuestionVoteService : IQuestionVoteService
{
    private readonly AppDbContext _context;

    public QuestionVoteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task VoteAsync(Guid userId, VoteRequest request)
    {
        var existingVote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == request.QuestionId && v.UserId == userId);

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
                UserId = userId,
                Value = request.Value
            });
        }

        await _context.RecalculateVotesCountAsync(request.QuestionId);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveVoteAsync(Guid id)
    {
        var vote = await _context.QuestionVotes.FindAsync(id);
        if (vote == null)
            return false;

        var questionId = vote.QuestionId;

        _context.QuestionVotes.Remove(vote);
        await _context.RecalculateVotesCountAsync(questionId);
        await _context.SaveChangesAsync();

        return true;
    }
}