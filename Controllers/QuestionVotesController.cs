using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionVotesController : ControllerBase
{
    private readonly IQuestionVoteService _voteService;

    public QuestionVotesController(IQuestionVoteService voteService)
    {
        _voteService = voteService;
    }

    [HttpPost]
    public async Task<IActionResult> Vote(VoteRequest request)
    {
        var userId = User.GetUserId();
        await _voteService.VoteAsync(userId, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveVote(Guid id)
    {
        var deleted = await _voteService.RemoveVoteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}