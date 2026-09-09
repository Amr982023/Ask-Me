using AskMe.Application.Common.Interfaces;
using AskMe.Application.Common.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace AskMe.Infrastructure.Identity;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleOptions _options;

    public GoogleTokenValidator(IOptions<GoogleOptions> options) => _options = options.Value;

    public async Task<ExternalUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _options.ClientId },
            });

            // payload.Sub is Google's stable, unique subject id for the account.
            return new ExternalUserInfo(payload.Subject, payload.Email, payload.Name);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
