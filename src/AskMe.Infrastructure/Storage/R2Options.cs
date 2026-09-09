namespace AskMe.Infrastructure.Storage;

public class R2Options
{
    public const string SectionName = "CloudflareR2";

    public required string AccountId { get; set; }
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string BucketName { get; set; }
    // Public base URL for the bucket - either an r2.dev URL or a custom domain
    // fronting it. Only this URL and object keys ever leave the backend.
    public required string PublicBaseUrl { get; set; }
}
