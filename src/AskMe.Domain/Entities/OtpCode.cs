using AskMe.Domain.Common;
using AskMe.Domain.Enums;

namespace AskMe.Domain.Entities;

// Short-lived one-time codes emailed to the user for registration
// verification and password reset. Codes are hashed at rest (same pattern as
// passwords) so a DB read alone can't be used to impersonate a pending
// verification/reset.
public class OtpCode : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string CodeHash { get; set; }
    public required OtpPurpose Purpose { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
