namespace CloneAmazonBack.Services;

public class EmailOptions
{
    public bool Enabled { get; set; }

    /// <summary>"Smtp" or "Resend" (HTTP API, works on hosts that block SMTP).</summary>
    public string Provider { get; set; } = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Treemiix";
    public string AppBaseUrl { get; set; } = "https://treemiix.com";
}
