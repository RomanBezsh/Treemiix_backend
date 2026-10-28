using System.Net;
using System.Net.Sockets;
using CloneAmazonBack.Data;
using CloneAmazonBack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    /// <summary>
    /// Media is stored in PostgreSQL, so per-file size is kept small on purpose.
    /// </summary>
    private const long MaxImageBytes = 5L * 1024 * 1024;

    private const string TooLargeMessage = "File is too large (max 5 MB)";

    private static readonly Dictionary<string, string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp"
    };

    private static readonly string[] VideoExtensions =
    {
        ".mp4", ".webm", ".mov", ".avi"
    };

    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public UploadController(AppDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public record ImportMediaRequest(string Url);

    /// <summary>Serves media stored in the database. Both URL shapes are kept for compatibility.</summary>
    [HttpGet("/media/{fileName}")]
    [HttpGet("/uploads/{fileName}")]
    [HttpHead("/media/{fileName}")]
    [HttpHead("/uploads/{fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 128)
            return NotFound();

        var media = await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.FileName == fileName)
            .Select(m => new { m.Id, m.ContentType, m.Bytes })
            .FirstOrDefaultAsync(cancellationToken);

        if (media is null)
            return NotFound();

        // Files are immutable (the name is a fresh GUID), so clients may cache forever.
        var etag = $"\"{media.Id}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        if (Request.Headers["If-None-Match"] == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        return File(media.Bytes, media.ContentType);
    }

    [HttpPost("import")]
    [Authorize]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public async Task<IActionResult> ImportFromUrl([FromBody] ImportMediaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Url))
            return BadRequest("Url is required");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest("Only http(s) URLs are allowed");

        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();

        if (VideoExtensions.Contains(extension))
            return BadRequest("Video uploads are not supported: media is stored in the database");

        if (!ImageContentTypes.ContainsKey(extension))
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

            if (response.Content.Headers.ContentLength is long length && length > MaxImageBytes)
                return BadRequest(TooLargeMessage);

            var bytes = await ReadWithLimitAsync(
                await response.Content.ReadAsStreamAsync(),
                MaxImageBytes);

            if (bytes is null)
                return BadRequest(TooLargeMessage);

            return await StoreAsync(bytes, extension, "media");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return BadRequest("Unable to download the file");
        }
    }

    /// <summary>Legacy endpoint kept for backwards compatibility.</summary>
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public Task<IActionResult> Upload(IFormFile? file) => SaveUploadAsync(file, "uploads");

    [HttpPost("media")]
    [Authorize]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public Task<IActionResult> UploadMedia(IFormFile? file) => SaveUploadAsync(file, "media");

    private async Task<IActionResult> SaveUploadAsync(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file");

        if (file.Length > MaxImageBytes)
            return BadRequest(TooLargeMessage);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (VideoExtensions.Contains(extension))
            return BadRequest("Video uploads are not supported: media is stored in the database");

        if (!ImageContentTypes.ContainsKey(extension))
            return BadRequest($"File type '{extension}' is not allowed");

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);

        return await StoreAsync(buffer.ToArray(), extension, folder);
    }

    private async Task<IActionResult> StoreAsync(byte[] bytes, string extension, string folder)
    {
        var fileName = Guid.NewGuid().ToString("N") + extension;

        _context.MediaFiles.Add(new MediaFile
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = ImageContentTypes[extension],
            Size = bytes.LongLength,
            Bytes = bytes,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new { path = $"/{folder}/{fileName}" });
    }

    private static async Task<byte[]?> ReadWithLimitAsync(Stream source, long maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;

        while ((read = await source.ReadAsync(chunk)) > 0)
        {
            total += read;
            if (total > maxBytes)
                return null;

            await buffer.WriteAsync(chunk.AsMemory(0, read));
        }

        return buffer.ToArray();
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
