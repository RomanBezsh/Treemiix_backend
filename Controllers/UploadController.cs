using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private const long MaxFileBytes = 200L * 1024 * 1024;

    private static readonly string[] AllowedMediaExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".mp4", ".webm", ".mov", ".avi"
    };

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
    [RequestSizeLimit(MaxFileBytes)]
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

        // Basic SSRF protection: never fetch loopback/private/link-local hosts.
        if (!await IsPublicHostAsync(uri.Host))
            return BadRequest("The provided URL points to a non-public host");

        try
        {
            var client = _httpClientFactory.CreateClient("media-import");

            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return BadRequest($"Unable to download the file (HTTP {(int)response.StatusCode})");

            if (response.Content.Headers.ContentLength is long length && length > MaxFileBytes)
                return BadRequest("File is too large (max 200 MB)");

            var uploadPath = Path.Combine(GetWebRootPath(), "media");
            Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid().ToString("N") + extension;
            var filePath = Path.Combine(uploadPath, fileName);

            var copied = await CopyWithLimitAsync(
                await response.Content.ReadAsStreamAsync(),
                filePath,
                MaxFileBytes);

            if (!copied)
            {
                System.IO.File.Delete(filePath);
                return BadRequest("File is too large (max 200 MB)");
            }

            return Ok(new { path = $"/media/{fileName}" });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return BadRequest("Unable to download the file");
        }
    }

    /// <summary>Legacy endpoint kept for backwards compatibility.</summary>
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(MaxFileBytes)]
    public Task<IActionResult> Upload(IFormFile file) => SaveUploadAsync(file, "uploads");

    [HttpPost("media")]
    [Authorize]
    [RequestSizeLimit(MaxFileBytes)]
    public Task<IActionResult> UploadMedia(IFormFile file) => SaveUploadAsync(file, "media");

    private async Task<IActionResult> SaveUploadAsync(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file");

        if (file.Length > MaxFileBytes)
            return BadRequest("File is too large (max 200 MB)");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedMediaExtensions.Contains(extension))
            return BadRequest($"File type '{extension}' is not allowed");

        var uploadPath = Path.Combine(GetWebRootPath(), folder);
        Directory.CreateDirectory(uploadPath);

        var fileName = Guid.NewGuid().ToString("N") + extension;
        var filePath = Path.Combine(uploadPath, fileName);

        await using (var stream = new FileStream(filePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { path = $"/{folder}/{fileName}" });
    }

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
            return _env.WebRootPath;

        return Path.Combine(_env.ContentRootPath, "wwwroot");
    }

    private static async Task<bool> CopyWithLimitAsync(Stream source, string destination, long maxBytes)
    {
        await using var target = new FileStream(destination, FileMode.CreateNew);
        var buffer = new byte[81920];
        long total = 0;
        int read;

        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            total += read;
            if (total > maxBytes)
                return false;

            await target.WriteAsync(buffer.AsMemory(0, read));
        }

        return true;
    }

    private static async Task<bool> IsPublicHostAsync(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IPAddress.TryParse(host, out var literal))
            return !IsPrivateAddress(literal);

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(address => !IsPrivateAddress(address));
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   bytes[0] == 0 ||
                   bytes[0] >= 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
                return true;

            var bytes = address.GetAddressBytes();
            // Unique local addresses (fc00::/7) and link-local (fe80::/10).
            return (bytes[0] & 0xFE) == 0xFC || (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80);
        }

        return true;
    }
}
