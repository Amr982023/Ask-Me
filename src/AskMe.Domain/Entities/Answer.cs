using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

public class Answer : BaseEntity
{
    public required Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public required string Content { get; set; }
    public string? ImageKey { get; set; }
    public string? YoutubeUrl { get; set; }

    // Optional link to one of the SAME owner's previous answers - lets them
    // point to a fuller answer they already gave instead of repeating
    // themselves. Restricted (not cascading) on delete: deleting an answer
    // that others reference should fail loudly rather than silently orphan
    // the reference - see AnswerConfiguration.
    public Guid? ReferencedAnswerId { get; set; }
    public Answer? ReferencedAnswer { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
