using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AskMe.Application.Common.Interfaces;
using AskMe.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace AskMe.Infrastructure.Storage;

// Cloudflare R2 is S3-compatible, so the AWS S3 SDK is used pointed at R2's
// endpoint. Credentials live only here/in configuration - React never sees
// them; only object keys and the public base URL are returned to clients.
public class CloudflareR2StorageService : IFileStorageService
{
    private readonly IAmazonS3 _client;
    private readonly R2Options _options;

    public CloudflareR2StorageService(IOptions<R2Options> options)
    {
        _options = options.Value;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
            });
    }
    public async Task<FileContent?> GetAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            using var response = await _client.GetObjectAsync(_options.BucketName, objectKey, ct);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);
            return new FileContent(ms.ToArray(), response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        ValidateImageContentType(contentType);

        var extension = Path.GetExtension(fileName);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
        }, ct);

        return key;
    }

    public string GetPublicUrl(string objectKey) => $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
        }, ct);
    }

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private static void ValidateImageContentType(string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new AskMe.Application.Common.Exceptions.ValidationException(
                "Only JPEG, PNG, WEBP, or GIF images are allowed.");
    }
}
