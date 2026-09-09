using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AskMe.Application.Common.Interfaces;
using AskMe.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace AskMe.Infrastructure.Identity;

public class FacebookTokenValidator : IFacebookTokenValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly FacebookOptions _options;

    public FacebookTokenValidator(HttpClient http, IOptions<FacebookOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<ExternalUserInfo?> ValidateAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        // 1. Confirm the token was actually issued to OUR app (prevents a
        // token minted for a different Facebook app being replayed here).
        var appAccessToken = $"{_options.AppId}|{_options.AppSecret}";
        var debugResponse = await _http.GetFromJsonAsync<FacebookDebugTokenResponse>(
            $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appAccessToken}",
            JsonOptions, cancellationToken);

        if (debugResponse?.Data is null || !debugResponse.Data.IsValid || debugResponse.Data.AppId != _options.AppId)
            return null;

        // 2. Fetch the actual profile fields we need.
        var profile = await _http.GetFromJsonAsync<FacebookProfileResponse>(
            $"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}",
            JsonOptions, cancellationToken);

        if (profile is null || string.IsNullOrEmpty(profile.Email))
            return null; // Facebook can omit email if the user didn't grant that permission.

        return new ExternalUserInfo(profile.Id, profile.Email, profile.Name);
    }

    private record FacebookDebugTokenResponse(FacebookDebugTokenData? Data);

    private record FacebookDebugTokenData(
        [property: JsonPropertyName("is_valid")] bool IsValid,
        [property: JsonPropertyName("app_id")] string AppId);

    private record FacebookProfileResponse(string Id, string? Name, string? Email);
}
