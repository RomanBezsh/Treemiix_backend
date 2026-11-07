using System.Net;
using System.Net.Http.Headers;
using CloneAmazonBack.Data;
using CloneAmazonBack.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloneAmazonBack.Tests;

public class DemoDataSeederTests
{
    private static DemoDataSeeder CreateSeeder(AppDbContext context)
    {
        var configuration = new ConfigurationBuilder().Build();

        return new DemoDataSeeder(
            context,
            new FakeHttpClientFactory(),
            configuration,
            NullLogger<DemoDataSeeder>.Instance);
    }

    [Fact]
    public async Task SeedAsync_CreatesMarketplaceDataAndIsIdempotent()
    {
        await using var context = TestDbContextFactory.Create();
        var seeder = CreateSeeder(context);

        var first = await seeder.SeedAsync(new DemoSeedOptions(ProductCount: 12));

        Assert.Equal(12, first.Products);
        Assert.Equal(10, first.Sellers);
        Assert.Equal(12, first.Customers);
        Assert.True(first.Images > 0);
        Assert.Equal(12, await context.Products.CountAsync());
        Assert.True(await context.ProductGalleries.AnyAsync());
        Assert.True(await context.ProductReviews.AnyAsync());
        Assert.True(await context.ProductQuestions.AnyAsync());
        Assert.True(await context.Carts.AnyAsync());
        Assert.True(await context.Orders.AnyAsync());
        Assert.Equal(5, await context.PromoCodes.CountAsync());
        Assert.Equal(5, await context.GiftCards.CountAsync());

        var productCount = await context.Products.CountAsync();
        var galleryCount = await context.ProductGalleries.CountAsync();
        var reviewCount = await context.ProductReviews.CountAsync();
        var orderCount = await context.Orders.CountAsync();
        var mediaCount = await context.MediaFiles.CountAsync();

        var second = await seeder.SeedAsync(new DemoSeedOptions(ProductCount: 12));

        Assert.Equal(12, second.Products);
        Assert.Equal(productCount, await context.Products.CountAsync());
        Assert.Equal(galleryCount, await context.ProductGalleries.CountAsync());
        Assert.Equal(reviewCount, await context.ProductReviews.CountAsync());
        Assert.Equal(orderCount, await context.Orders.CountAsync());
        Assert.Equal(mediaCount, await context.MediaFiles.CountAsync());
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client = new(new FakeImageHandler());

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeImageHandler : HttpMessageHandler
    {
        private static readonly byte[] TinyJpeg = [0xFF, 0xD8, 0xFF, 0xD9];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(TinyJpeg);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            });
        }
    }
}
