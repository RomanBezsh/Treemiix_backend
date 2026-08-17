using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PromoCodeProductsController : ControllerBase
{
    private readonly IPromoCodeProductService _linkService;

    public PromoCodeProductsController(IPromoCodeProductService linkService)
    {
        _linkService = linkService;
    }

    [HttpGet("bypromocode/{promoCodeId}")]
    public async Task<IActionResult> GetByPromoCode(Guid promoCodeId)
    {
        var items = await _linkService.GetByPromoCodeAsync(promoCodeId);
        return Ok(items);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePromoCodeProductRequest request)
    {
        var link = await _linkService.CreateAsync(request);
        return Created($"/api/promocodeproducts/bypromocode/{link.PromoCodeId}", link);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _linkService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}