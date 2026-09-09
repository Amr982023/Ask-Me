using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

// An IP an admin has banned from creating new accounts - checked at
// registration and at first-time OAuth sign-up. Deleting an abusive user
// doesn't stop them registering again from the same network without this.
public class BlockedIp : BaseEntity
{
    public required string IpAddress { get; set; }
    public string? Reason { get; set; }
    public required Guid BlockedByAdminId { get; set; }
}
