using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

// Holds a registration's data until the emailed OTP is verified - the real
// User row is only created at that point. Before this existed, an
// abandoned/never-verified registration would still occupy its username and
// email permanently, since the User row was created immediately at
// RegisterAsync and just left with EmailConfirmed = false forever.
public class PendingRegistration : BaseEntity
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public string? RegistrationIp { get; set; }

    public required string CodeHash { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
