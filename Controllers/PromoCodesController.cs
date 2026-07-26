using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromoCodesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PromoCodesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var codes = await _context.PromoCodes.ToListAsync();
        return Ok(codes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var code = await _context.PromoCodes.FindAsync(id);
        if (code == null) return NotFound();
        return Ok(code);
    }

    [AllowAnonymous]
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code == code);
        if (promo == null) return NotFound();
        return Ok(promo);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePromoCodeRequest request)
    {
        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            DiscountValue = request.DiscountValue,
            DiscountType = request.DiscountType,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MaxActivations = request.MaxActivations,
            LimitPerUser = request.LimitPerUser,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt,
            IsActive = true
        };

        _context.PromoCodes.Add(promo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = promo.Id }, promo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreatePromoCodeRequest request)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound();

        promo.DiscountValue = request.DiscountValue;
        promo.DiscountType = request.DiscountType;
        promo.MinOrderAmount = request.MinOrderAmount;
        promo.MaxDiscountAmount = request.MaxDiscountAmount;
        promo.MaxActivations = request.MaxActivations;
        promo.LimitPerUser = request.LimitPerUser;
        promo.StartsAt = request.StartsAt;
        promo.ExpiresAt = request.ExpiresAt;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var promo = await _context.PromoCodes.FindAsync(id);
        if (promo == null) return NotFound();

        _context.PromoCodes.Remove(promo);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
