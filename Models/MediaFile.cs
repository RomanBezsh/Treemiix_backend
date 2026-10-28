namespace CloneAmazonBack.Models;

/// <summary>
/// Binary media stored in PostgreSQL. The container filesystem on Render (and most
/// PaaS providers) is ephemeral, so files written to wwwroot disappear on every
/// deploy. Keeping the bytes in the database makes uploads survive restarts.
/// </summary>
public class MediaFile
{
    public Guid Id { get; set; }

    /// <summary>Name used in the public URL, e.g. "b48e5b8a78024....jpg".</summary>
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    public byte[] Bytes { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
