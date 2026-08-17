using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromoCodesController : ControllerBase
{
    private readonly IPromoCodeService _promoCodeService;

    public PromoCodesController(IPromoCodeService promoCodeService)
    {
        _promoCodeService = promoCodeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var codes = await _promoCodeService.GetAllAsync();
        return Ok(codes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var code = await _promoCodeService.GetByIdAsync(id);
        if (code == null) return NotFound();
        return Ok(code);
    }

    [AllowAnonymous]
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var promo = await _promoCodeService.GetByCodeAsync(code);
        if (promo == null) return NotFound();
        return Ok(promo);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePromoCodeRequest request)
    {
        var promo = await _promoCodeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = promo.Id }, promo);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreatePromoCodeRequest request)
    {
        await _promoCodeService.UpdateAsync(id, request);
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _promoCodeService.DeleteAsync(id);
        return NoContent();
    }
}