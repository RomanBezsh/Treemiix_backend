using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;

    public UploadController(IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
    {
        _env = env;
        _httpClientFactory = httpClientFactory;
    }

    public record ImportMediaRequest(string Url);

    [HttpPost("import")]
    [Authorize]
    public async Task<IActionResult> ImportFromUrl([FromBody] ImportMediaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Url))
            return BadRequest("Url is required");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest("Only http(s) URLs are allowed");

        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedMediaExtensions.Contains(extension))
            return BadRequest($"File type '{extension}' is not allowed");

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return BadRequest($"Unable to download the file (HTTP {(int)response.StatusCode})");

            const long maxBytes = 200L * 1024 * 1024;
            if (response.Content.Headers.ContentLength is long length && length > maxBytes)
                return BadRequest("File is too large (max 200 MB)");

            var uploadPath = Path.Combine(_env.WebRootPath, "media");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await (await response.Content.ReadAsStreamAsync()).CopyToAsync(stream);
            }

            return Ok(new { path = $"/media/{fileName}" });
        }
        catch (Exception ex)
        {
            return BadRequest($"Unable to download the file: {ex.Message}");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file");
        
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Возвращаем путь к файлу, который будет доступен через /uploads/...
        return Ok(new { path = $"/uploads/{fileName}" });
    }

    private static readonly string[] AllowedMediaExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg",
        ".mp4", ".webm", ".mov", ".avi"
    };

    [HttpPost("media")]
    [Authorize]
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedMediaExtensions.Contains(extension))
            return BadRequest($"File type '{extension}' is not allowed");

        var uploadPath = Path.Combine(_env.WebRootPath, "media");
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        var fileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Файл будет доступен по адресу /media/...
        return Ok(new { path = $"/media/{fileName}" });
    }
}
