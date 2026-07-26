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
public class GiftCardsController : ControllerBase
{
    private readonly AppDbContext _context;

    public GiftCardsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.GetUserId();
        var cards = await _context.GiftCards
            .Where(g => g.PurchasedByUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return Ok(cards);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var card = await _context.GiftCards.FindAsync(id);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [AllowAnonymous]
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var card = await _context.GiftCards.FirstOrDefaultAsync(g => g.Code == code);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGiftCardRequest request)
    {
        var userId = User.GetUserId();
        var card = new GiftCard
        {
            Id = Guid.NewGuid(),
            Code = AppDbContextExtensions.GenerateGiftCardCode(),
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            PurchasedByUserId = userId,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.GiftCards.Add(card);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var userId = User.GetUserId();
        var card = await _context.GiftCards.FindAsync(id);
        if (card == null) return NotFound();

        if (!card.IsActive || card.CurrentBalance <= 0)
            return BadRequest("Gift card is not active or has no balance");

        if (card.ActivatedByUserId.HasValue)
            return BadRequest("Gift card already activated");

        card.ActivatedByUserId = userId;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var card = await _context.GiftCards.FindAsync(id);
        if (card == null) return NotFound();

        card.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
