using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

// A single public follow-up message on an already-answered question -
// intentionally one flat list per question, not a nested comment tree.
public class QuestionFollowUp : BaseEntity
{
    public required Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public required string Content { get; set; }
}
