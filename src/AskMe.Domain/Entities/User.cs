using AskMe.Domain.Common;
using AskMe.Domain.Enums;

namespace AskMe.Domain.Entities;

public class User : BaseEntity
{
    public required string Username { get; set; }
    public required string Email { get; set; }

    // Nullable: users who sign up via Google/Facebook have no local password.
    public string? PasswordHash { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AboutMe { get; set; }
    public string? ProfileImageKey { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Social links - all optional, all plain URLs.
    public string? WebsiteUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? TiktokUrl { get; set; }

    // Email/OTP verification - registration is not usable for login until true.
    public bool EmailConfirmed { get; set; } = false;

    // Set when the account was created/linked via an external provider.
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }

    // Captured once at account creation (password or OAuth) - shown to
    // admins so they know what to pass to the IP-block endpoint after
    // removing an abusive account.
    public string? RegistrationIp { get; set; }

    public ICollection<Question> QuestionsReceived { get; set; } = new List<Question>();
    public ICollection<Question> QuestionsAuthored { get; set; } = new List<Question>();
}
