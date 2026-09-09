namespace AskMe.Application.Common.Models;

// Normalized result of validating a Google/Facebook token, regardless of
// which provider produced it.
public record ExternalUserInfo(string ProviderUserId, string Email, string? Name);
