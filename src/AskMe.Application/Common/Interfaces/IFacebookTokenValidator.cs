using AskMe.Application.Common.Models;

namespace AskMe.Application.Common.Interfaces;

public interface IFacebookTokenValidator
{
    /// Validates a Facebook access token obtained by the frontend's FB SDK login. Returns null if invalid.
    Task<ExternalUserInfo?> ValidateAsync(string accessToken, CancellationToken cancellationToken = default);
}
