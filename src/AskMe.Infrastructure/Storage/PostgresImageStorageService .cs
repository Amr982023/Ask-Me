using AskMe.Application.Common.Exceptions;
using AskMe.Application.Common.Interfaces;
using AskMe.Application.Common.Models;
using AskMe.Domain.Entities;
using AskMe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AskMe.Infrastructure.Storage;

// MVP/testing-phase replacement for CloudflareR2StorageService: images are
// stored as raw bytes (bytea) directly in PostgreSQL instead of an external
// object store. Same IFileStorageService contract, so nothing in the
// Application layer changes - swapping back to R2 later is a one-line change
// in DependencyInjection.cs. CloudflareR2StorageService itself is left in
// place, unregistered, for that future move.
public class PostgresImageStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private readonly ApplicationDbContext _db;
    private readonly PostgresImageStorageOptions _options;

    public PostgresImageStorageService(ApplicationDbContext db, IOptions<PostgresImageStorageOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        ValidateImageContentType(contentType);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var image = new StoredImage
        {
            Data = buffer.ToArray(), // raw binary - never Base64/JSON-encoded
            ContentType = contentType,
        };

        _db.StoredImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);

        // The row's own Id is the "object key" that User.ProfileImageKey /
        // Answer.ImageKey persist - identical contract to the R2 implementation.
        return image.Id.ToString();
    }

    public string GetPublicUrl(string objectKey) => $"{_options.PublicBaseUrl.TrimEnd('/')}/api/images/{objectKey}";

    public async Task<FileContent?> GetAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(objectKey, out var id))
            return null;

        var image = await _db.StoredImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return image is null ? null : new FileContent(image.Data, image.ContentType);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(objectKey, out var id))
            return;

        var image = await _db.StoredImages.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (image is not null)
        {
            _db.StoredImages.Remove(image);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ValidateImageContentType(string contentType)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new ValidationException("Only JPEG, PNG, WEBP, or GIF images are allowed.");
    }
}