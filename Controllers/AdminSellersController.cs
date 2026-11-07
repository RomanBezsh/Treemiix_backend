using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/admin/sellers")]
[Authorize(Roles = Roles.Admin)]
public class AdminSellersController : ControllerBase
{
    private readonly ISellerService _sellerService;

    public AdminSellersController(ISellerService sellerService)
    {
        _sellerService = sellerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SellerStatus? status)
    {
        var sellers = await _sellerService.GetAllForAdminAsync(status);
        return Ok(sellers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var seller = await _sellerService.GetAdminByIdAsync(id);
        if (seller is null)
            return NotFound();

        return Ok(seller);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var seller = await _sellerService.ApproveAsync(id);
            return Ok(seller);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectSellerRequest request)
    {
        try
        {
            var seller = await _sellerService.RejectAsync(id, request.Reason);
            return Ok(seller);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(Guid id)
    {
        try
        {
            var seller = await _sellerService.SuspendAsync(id);
            return Ok(seller);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        try
        {
            var seller = await _sellerService.ReactivateAsync(id);
            return Ok(seller);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/subscription/paid")]
    public async Task<IActionResult> MarkSubscriptionPaid(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MarkSellerSubscriptionPaidRequest? request)
    {
        try
        {
            var seller = await _sellerService.MarkSubscriptionPaidAsync(id, request?.PaidUntil);
            return Ok(seller);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
