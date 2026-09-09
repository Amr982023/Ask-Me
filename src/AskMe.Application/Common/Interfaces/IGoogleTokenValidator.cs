using AskMe.Application.Common.Models;

namespace AskMe.Application.Common.Interfaces;

public interface IGoogleTokenValidator
{
    /// Validates a Google Identity Services ID token. Returns null if invalid/expired.
    Task<ExternalUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
