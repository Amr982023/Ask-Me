using AskMe.Application.Common.Models;

namespace AskMe.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    string GetPublicUrl(string objectKey);

    /// Retrieves the raw bytes and content type for a previously-uploaded
    /// object key, or null if it doesn't exist. Lets callers (e.g. an
    /// image-serving endpoint) stay backend-agnostic instead of depending on
    /// a specific storage implementation's persistence details.
    Task<FileContent?> GetAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}