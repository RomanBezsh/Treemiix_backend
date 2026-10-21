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
public class ProductAnswersController : ControllerBase
{
    private readonly IProductAnswerService _answerService;

    public ProductAnswersController(IProductAnswerService answerService)
    {
        _answerService = answerService;
    }

    [AllowAnonymous]
    [HttpGet("byquestion/{questionId}")]
    public async Task<IActionResult> GetByQuestion(Guid questionId)
    {
        var answers = await _answerService.GetByQuestionAsync(questionId);
        return Ok(answers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAnswerRequest request)
    {
        var userId = User.GetUserId();

        // Only sellers/admins may mark an answer as official.
        if (request.IsOfficialAnswer &&
            !User.IsInRole(Roles.Admin) &&
            !User.IsInRole(Roles.Seller))
        {
            request = request with { IsOfficialAnswer = false };
        }

        var answer = await _answerService.CreateAsync(userId, request);
        return Created($"/api/productanswers/byquestion/{answer.QuestionId}", answer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateAnswerRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);
        var updated = await _answerService.UpdateAsync(id, request.Content, userId, isAdmin);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);
        var deleted = await _answerService.DeleteAsync(id, userId, isAdmin);
        if (!deleted) return NotFound();
        return NoContent();
    }
}