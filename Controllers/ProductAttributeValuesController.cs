using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductAttributeValuesController : ControllerBase
{
    private readonly IProductAttributeValueService _attributeService;

    public ProductAttributeValuesController(IProductAttributeValueService attributeService)
    {
        _attributeService = attributeService;
    }

    [AllowAnonymous]
    [HttpGet("byproduct/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var attributes = await _attributeService.GetByProductAsync(productId);
        return Ok(attributes);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateAttributeRequest request)
    {
        var attribute = await _attributeService.CreateAsync(request);
        return Created($"/api/productattributevalues/byproduct/{attribute.ProductId}", attribute);
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateAttributeRequest request)
    {
        var updated = await _attributeService.UpdateAsync(id, request);
        if (!updated) return NotFound();
        return NoContent();
    }

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _attributeService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}