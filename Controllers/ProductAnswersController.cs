using CloneAmazonBack.Extensions;
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
        var answer = await _answerService.CreateAsync(userId, request);
        return Created($"/api/productanswers/byquestion/{answer.QuestionId}", answer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, string content)
    {
        var updated = await _answerService.UpdateAsync(id, content);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _answerService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}