namespace AskMe.Application.Users;

public record SocialLinks(
    string? WebsiteUrl, string? TwitterUrl, string? FacebookUrl,
    string? GithubUrl, string? LinkedinUrl, string? YoutubeUrl, string? TiktokUrl);

public record PublicProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? Bio,
    string? AboutMe,
    string? ProfileImageUrl,
    int QuestionCount,
    int TotalAnswers,
    int TotalVotes,
    SocialLinks SocialLinks
);

public record UpdateProfileRequest(
    string Username, string DisplayName, string? Bio, string? AboutMe,
    string? WebsiteUrl, string? TwitterUrl, string? FacebookUrl,
    string? GithubUrl, string? LinkedinUrl, string? YoutubeUrl, string? TiktokUrl
);

public record SearchResultDto(string Username, string DisplayName, string? ProfileImageUrl);

public record MeDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string? Bio,
    string? AboutMe,
    string? ProfileImageUrl,
    string Role,
    bool HasPassword,
    SocialLinks SocialLinks
);
