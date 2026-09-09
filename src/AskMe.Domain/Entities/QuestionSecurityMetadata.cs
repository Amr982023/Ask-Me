using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

public class QuestionSecurityMetadata : BaseEntity
{
    public required Guid QuestionId { get; set; }
    public Question? Question { get; set; }

    public required string SourceIp { get; set; }
    public string? UserAgent { get; set; }
    public Guid? AuthenticatedUserId { get; set; }
    public string? SessionIdentifier { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
