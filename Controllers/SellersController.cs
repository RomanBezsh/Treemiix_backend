using CloneAmazonBack.Extensions;
using CloneAmazonBack.Models;
using CloneAmazonBack.Models.Dtos;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SellersController : ControllerBase
{
    private readonly ISellerService _sellerService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public SellersController(
        ISellerService sellerService,
        IAuthService authService,
        IConfiguration configuration)
    {
        _sellerService = sellerService;
        _authService = authService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sellers = await _sellerService.GetAllAsync();
        return Ok(sellers);
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public IActionResult GetPlans()
    {
        var plan = new SellerPlanResponse(
            _configuration["Seller:Subscription:PlanName"] ?? "Professional",
            _configuration.GetValue("Seller:Subscription:MonthlyPrice", 39.99m),
            _configuration["Seller:Subscription:Currency"] ?? "USD",
            _configuration.GetValue("Seller:Subscription:PeriodDays", 30));

        return Ok(plan);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var seller = await _sellerService.GetByIdAsync(id);
        if (seller is null || seller.Status != SellerStatus.Active)
            return NotFound();

        return Ok(seller);
    }

    [AllowAnonymous]
    [HttpGet("byuser/{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var seller = await _sellerService.GetByUserAsync(userId);
        if (seller is null || seller.Status != SellerStatus.Active)
            return NotFound();

        return Ok(seller);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var seller = await _sellerService.GetByUserAsync(User.GetUserId());
        if (seller is null)
            return NotFound();

        return Ok(ToPrivateResponse(seller));
    }

    [HttpPost("onboarding/start")]
    public async Task<IActionResult> StartOnboarding()
    {
        try
        {
            var seller = await _sellerService.StartOnboardingAsync(User.GetUserId());

            // The role changed from User to Seller, so the client needs a fresh JWT.
            var auth = await _authService.IssueTokensForUserAsync(User.GetUserId());

            return Ok(new { seller = ToPrivateResponse(seller), auth });
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

    [HttpPut("onboarding/business")]
    public async Task<IActionResult> UpdateBusiness(UpdateSellerBusinessRequest request)
    {
        try
        {
            var seller = await _sellerService.UpdateBusinessAsync(User.GetUserId(), request);
            return Ok(ToPrivateResponse(seller));
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

    [HttpPut("onboarding/storefront")]
    public async Task<IActionResult> UpdateStorefront(UpdateSellerStorefrontRequest request)
    {
        try
        {
            var seller = await _sellerService.UpdateStorefrontAsync(User.GetUserId(), request);
            return Ok(ToPrivateResponse(seller));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("onboarding/payout")]
    public async Task<IActionResult> UpdatePayout(UpdateSellerPayoutRequest request)
    {
        try
        {
            var seller = await _sellerService.UpdatePayoutAsync(User.GetUserId(), request);
            return Ok(ToPrivateResponse(seller));
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

    [HttpPost("onboarding/submit")]
    public async Task<IActionResult> SubmitOnboarding()
    {
        try
        {
            var seller = await _sellerService.SubmitAsync(User.GetUserId());
            return Ok(new
            {
                message = "Application submitted for verification",
                seller = ToPrivateResponse(seller)
            });
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

    [Authorize(Roles = $"{Roles.Seller},{Roles.Admin}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateSellerStorefrontRequest request)
    {
        var existing = await _sellerService.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (!User.IsInRole(Roles.Admin) && existing.UserId != User.GetUserId())
            return Forbid();

        try
        {
            await _sellerService.UpdateAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private static SellerPrivateResponse ToPrivateResponse(Seller seller) => new(
        seller.Id,
        seller.UserId,
        seller.StoreName,
        seller.StoreSlug,
        seller.LogoUrl,
        seller.Description,
        seller.BusinessType,
        seller.LegalName,
        seller.Country,
        seller.SupportEmail,
        seller.SupportPhone,
        seller.TaxNumber,
        seller.LegalAddress,
        seller.BankAccount,
        seller.Rating,
        seller.Status,
        seller.CommissionRate,
        seller.OnboardingStep,
        seller.CreatedAt,
        seller.SubmittedAt,
        seller.ApprovedAt,
        seller.RejectedAt,
        seller.RejectionReason,
        seller.SubscriptionStatus,
        seller.SubscriptionPaidUntil,
        seller.CanSell);
}
