using CloneAmazonBack.Models;
using CloneAmazonBack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/admin/demo")]
[Authorize(Roles = Roles.Admin)]
public class AdminDemoController : ControllerBase
{
    private readonly DemoDataSeeder _demoDataSeeder;
    private readonly IConfiguration _configuration;

    public AdminDemoController(DemoDataSeeder demoDataSeeder, IConfiguration configuration)
    {
        _demoDataSeeder = demoDataSeeder;
        _configuration = configuration;
    }

    /// <summary>
    /// Fills the database with a realistic demo marketplace. Admin-only.
    /// Use reset=true to remove only rows created by this seeder before recreating them.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(
        [FromQuery] int? productCount,
        [FromQuery] bool reset,
        CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue("Database:AllowDemoSeed", true))
            return NotFound();

        var configuredCount = _configuration.GetValue("Database:DemoSeedProductCount", 60);
        var requestedCount = productCount ?? configuredCount;
        var result = await _demoDataSeeder.SeedAsync(
            new DemoSeedOptions(requestedCount, reset),
            cancellationToken);

        return Ok(result);
    }
}
