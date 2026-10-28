using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CloneAmazonBack.Services;

/// <summary>
/// Sends email through the Resend HTTP API. Preferred on Render because many
/// PaaS providers block outbound SMTP ports.
/// </summary>
public class ResendEmailSender : IEmailSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        IHttpClientFactory httpClientFactory,
        IOptions<EmailOptions> options,
        ILogger<ResendEmailSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Resend email delivery is not configured");
        }

        var client = _httpClientFactory.CreateClient("email");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from = $"{_options.FromName} <{_options.FromAddress}>",
                to = new[] { to },
                subject,
                html = htmlBody
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Resend API returned {(int)response.StatusCode}: {body}");
        }

        _logger.LogInformation("Email {Subject} sent to {Recipient} via Resend", subject, to);
    }
}
