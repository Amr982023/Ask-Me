using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

public class Question : BaseEntity
{
    public required Guid TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    public Guid? AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }

    public required string Content { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public int VoteCount { get; set; } = 0;

    public Answer? Answer { get; set; }
    public QuestionSecurityMetadata? SecurityMetadata { get; set; }
    public ICollection<QuestionVote> Votes { get; set; } = new List<QuestionVote>();
    public ICollection<QuestionFollowUp> FollowUps { get; set; } = new List<QuestionFollowUp>();
}
