using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GiftCardsController : ControllerBase
{
    private readonly IGiftCardService _giftCardService;

    public GiftCardsController(IGiftCardService giftCardService)
    {
        _giftCardService = giftCardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.GetUserId();
        var cards = await _giftCardService.GetAllAsync(userId);
        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var card = await _giftCardService.GetByIdAsync(id);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [AllowAnonymous]
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var card = await _giftCardService.GetByCodeAsync(code);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGiftCardRequest request)
    {
        var userId = User.GetUserId();
        var card = await _giftCardService.CreateAsync(userId, request);

        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var userId = User.GetUserId();

        try
        {
            await _giftCardService.ActivateAsync(id, userId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _giftCardService.DeleteAsync(id);
        return NoContent();
    }
}