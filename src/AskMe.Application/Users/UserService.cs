using AskMe.Application.Common.Exceptions;
using AskMe.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Application.Users;

public class UserService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorageService _storage;

    public UserService(IApplicationDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PublicProfileDto> GetPublicProfileAsync(string username, CancellationToken ct = default)
    {
        var uname = username.Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == uname, ct)
            ?? throw new NotFoundException("User", username);

        var visibleQuestions = _db.Questions.AsNoTracking()
            .Where(q => q.TargetUserId == user.Id && q.IsVisible && !q.IsDeleted);

        var questionCount = await visibleQuestions.CountAsync(ct);
        var totalAnswers = await visibleQuestions.CountAsync(q => q.Answer != null, ct);
        var totalVotes = await visibleQuestions.SumAsync(q => (int?)q.VoteCount, ct) ?? 0;

        return new PublicProfileDto(
            user.Id, user.Username, user.DisplayName, user.Bio, user.AboutMe,
            user.ProfileImageKey is null ? null : _storage.GetPublicUrl(user.ProfileImageKey),
            questionCount, totalAnswers, totalVotes,
            new SocialLinks(user.WebsiteUrl, user.TwitterUrl, user.FacebookUrl,
                user.GithubUrl, user.LinkedinUrl, user.YoutubeUrl, user.TiktokUrl));
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        return new MeDto(
            user.Id, user.Username, user.Email, user.DisplayName, user.Bio, user.AboutMe,
            user.ProfileImageKey is null ? null : _storage.GetPublicUrl(user.ProfileImageKey),
            user.Role.ToString(), user.PasswordHash is not null,
            new SocialLinks(user.WebsiteUrl, user.TwitterUrl, user.FacebookUrl,
                user.GithubUrl, user.LinkedinUrl, user.YoutubeUrl, user.TiktokUrl));
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new ValidationException("Display name is required.");

        var newUsername = request.Username.Trim().ToLowerInvariant();
        if (newUsername != user.Username)
        {
            if (newUsername.Length is < 3 or > 30)
                throw new ValidationException("Username must be between 3 and 30 characters.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(newUsername, "^[a-z0-9_]+$"))
                throw new ValidationException("Username may only contain lowercase letters, numbers and underscores.");

            var taken = await _db.Users.AnyAsync(u => u.Id != userId && u.Username == newUsername, ct);
            if (taken)
                throw new ValidationException("That username is already taken.");

            user.Username = newUsername;
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Bio = request.Bio?.Trim();
        user.AboutMe = request.AboutMe?.Trim();
        user.WebsiteUrl = NormalizeUrl(request.WebsiteUrl);
        user.TwitterUrl = NormalizeUrl(request.TwitterUrl);
        user.FacebookUrl = NormalizeUrl(request.FacebookUrl);
        user.GithubUrl = NormalizeUrl(request.GithubUrl);
        user.LinkedinUrl = NormalizeUrl(request.LinkedinUrl);
        user.YoutubeUrl = NormalizeUrl(request.YoutubeUrl);
        user.TiktokUrl = NormalizeUrl(request.TiktokUrl);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// Simple prefix/substring username search for the main-page search bar.
    /// Small, fixed result cap - not paginated, this is a "find one person" tool.
    public async Task<List<SearchResultDto>> SearchProfilesAsync(string query, CancellationToken ct = default)
    {
        var q = query.Trim().ToLowerInvariant();
        if (q.Length < 2) return new List<SearchResultDto>();

        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Username.Contains(q))
            .OrderBy(u => u.Username.StartsWith(q) ? 0 : 1) // exact-prefix matches first
            .ThenBy(u => u.Username)
            .Take(10)
            .ToListAsync(ct);

        return users.Select(u => new SearchResultDto(
            u.Username, u.DisplayName,
            u.ProfileImageKey is null ? null : _storage.GetPublicUrl(u.ProfileImageKey)
        )).ToList();
    }

    public async Task UpdateProfileImageAsync(Guid userId, Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        var oldKey = user.ProfileImageKey;
        var key = await _storage.UploadAsync(content, fileName, contentType, ct);
        user.ProfileImageKey = key;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldKey))
            await _storage.DeleteAsync(oldKey, ct);
    }

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        // Be forgiving of "twitter.com/x" style input without a scheme.
        if (!trimmed.StartsWith("http://") && !trimmed.StartsWith("https://"))
            trimmed = "https://" + trimmed;
        return Uri.TryCreate(trimmed, UriKind.Absolute, out _)
            ? trimmed
            : throw new ValidationException($"'{url}' isn't a valid URL.");
    }
}
