using AskMe.Domain.Common;
using AskMe.Domain.Enums;

namespace AskMe.Domain.Entities;

public class SecurityAuditLog : BaseEntity
{
    public required Guid AdminUserId { get; set; }

    // Nullable - actions like DeleteUser/BlockIp/UnblockIp aren't tied to a
    // specific question.
    public Guid? QuestionId { get; set; }

    public required SecurityAction Action { get; set; }
    public string? Notes { get; set; }
}
