using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? sellerId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 20;

        if (pageSize > 100)
            pageSize = 100;

        var result = await _productService.GetAllAsync(
            categoryId,
            sellerId,
            isActive,
            page,
            pageSize);

        var totalPages = (int)Math.Ceiling(
            result.TotalCount / (double)pageSize);

        return Ok(new
        {
            items = result.Items,
            page,
            pageSize,
            totalCount = result.TotalCount,
            totalPages
        });
    }

    [AllowAnonymous]
    [HttpGet("deals")]
    public async Task<IActionResult> GetDeals()
    {
        var products = await _productService.GetDealsAsync();
        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateProductRequest request)
    {
        await _productService.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        await _productService.SoftDeleteAsync(id);
        return NoContent();
    }

    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(Guid id)
    {
        await _productService.HardDeleteAsync(id);
        return NoContent();
    }
}