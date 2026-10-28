using CloneAmazonBack.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloneAmazonBack.Tests;

public class MediaStorageTests
{
    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private static UploadController CreateController(Data.AppDbContext context)
    {
        return new UploadController(context, new StubHttpClientFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static IFormFile CreateFormFile(byte[] bytes, string fileName, string contentType)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static string PathOf(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var property = ok.Value!.GetType().GetProperty("path");

        return Assert.IsType<string>(property!.GetValue(ok.Value));
    }

    private static string FileNameOf(string path) => path["/media/".Length..];

    [Fact]
    public async Task UploadMedia_ShouldStoreImageInDatabase()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var bytes = new byte[] { 1, 2, 3, 4, 5 };

        var path = PathOf(await controller.UploadMedia(CreateFormFile(bytes, "photo.jpg", "image/jpeg")));

        Assert.StartsWith("/media/", path);
        Assert.EndsWith(".jpg", path);

        var stored = Assert.Single(context.MediaFiles);
        Assert.Equal(FileNameOf(path), stored.FileName);
        Assert.Equal("image/jpeg", stored.ContentType);
        Assert.Equal(bytes.Length, stored.Size);
        Assert.Equal(bytes, stored.Bytes);
    }

    [Fact]
    public async Task UploadMedia_ShouldIgnoreSpoofedContentType()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        await controller.UploadMedia(CreateFormFile([1, 2, 3], "photo.png", "text/html"));

        Assert.Equal("image/png", Assert.Single(context.MediaFiles).ContentType);
    }

    [Fact]
    public async Task GetMedia_ShouldReturnStoredBytesWithImmutableCaching()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var bytes = new byte[] { 9, 8, 7 };

        var path = PathOf(await controller.UploadMedia(CreateFormFile(bytes, "photo.png", "image/png")));

        var result = await controller.GetMedia(FileNameOf(path), CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(bytes, file.FileContents);
        Assert.Equal("public, max-age=31536000, immutable", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetMedia_WithMatchingETag_ShouldReturnNotModified()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var path = PathOf(await controller.UploadMedia(CreateFormFile([1], "photo.webp", "image/webp")));
        var stored = Assert.Single(context.MediaFiles);

        controller.Request.Headers["If-None-Match"] = $"\"{stored.Id}\"";

        var result = await controller.GetMedia(FileNameOf(path), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Fact]
    public async Task GetMedia_WithUnknownFile_ShouldReturnNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.GetMedia("missing.jpg", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadMedia_WithVideo_ShouldBeRejected()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.UploadMedia(CreateFormFile([1, 2], "clip.mp4", "video/mp4"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.MediaFiles);
    }

    [Fact]
    public async Task UploadMedia_WithUnsupportedExtension_ShouldBeRejected()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.UploadMedia(CreateFormFile([1, 2], "payload.svg", "image/svg+xml"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.MediaFiles);
    }

    [Fact]
    public async Task UploadMedia_WithoutFile_ShouldBeRejected()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.UploadMedia(null);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.MediaFiles);
    }
}
