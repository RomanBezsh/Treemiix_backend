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
        var questionExists = await _context.ProductQuestions
            .AnyAsync(q => q.Id == request.QuestionId);

        if (!questionExists)
            throw new InvalidOperationException("Question not found");

        var existingVote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.QuestionId == request.QuestionId && v.UserId == userId);

        // A zero value means "remove my vote".
        if (request.Value == 0)
        {
            if (existingVote is null)
                return;

            _context.QuestionVotes.Remove(existingVote);
        }
        else if (existingVote is not null)
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

        // Persist the vote first: the recalculation query must see the new state.
        await _context.SaveChangesAsync();
        await _context.RecalculateVotesCountAsync(request.QuestionId);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveVoteAsync(Guid id, Guid userId)
    {
        var vote = await _context.QuestionVotes
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);
        if (vote == null)
            return false;

        var questionId = vote.QuestionId;

        _context.QuestionVotes.Remove(vote);
        await _context.SaveChangesAsync();
        await _context.RecalculateVotesCountAsync(questionId);
        await _context.SaveChangesAsync();

        return true;
    }
}
