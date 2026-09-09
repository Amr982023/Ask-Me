using AskMe.Domain.Common;

namespace AskMe.Domain.Entities;

public class QuestionVote : BaseEntity
{
    public required Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public required string IpAddress { get; set; }
}
