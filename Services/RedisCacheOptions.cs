namespace CloneAmazonBack.Services;

public class RedisCacheOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "CloneAmazonBack:";
    public int DefaultExpiryMinutes { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}