using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductQuestionsController : ControllerBase
{
    private readonly IProductQuestionService _questionService;

    public ProductQuestionsController(IProductQuestionService questionService)
    {
        _questionService = questionService;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var questions = await _questionService.GetByProductAsync(productId);
        return Ok(questions);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var question = await _questionService.GetByIdAsync(id);
        if (question == null) return NotFound();
        return Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateQuestionRequest request)
    {
        var userId = User.GetUserId();
        var question = await _questionService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _questionService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}