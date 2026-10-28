using CloneAmazonBack.Services.Interfaces;

namespace CloneAmazonBack.Services;

/// <summary>Development fallback: prints the email to the application log.</summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Email delivery is disabled. Message for {Recipient}: {Subject}\n{Body}",
            to,
            subject,
            htmlBody);

        return Task.CompletedTask;
    }
}

/// <summary>Production fallback used when SMTP is not configured.</summary>
public class UnconfiguredEmailSender : IEmailSender
{
    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Email delivery is not configured. Set the Email:* configuration values.");
    }
}

/// <summary>No-op sender for unit tests.</summary>
public class NullEmailSender : IEmailSender
{
    public static readonly NullEmailSender Instance = new();

    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
