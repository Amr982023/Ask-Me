namespace AskMe.Application.Questions;

public enum QuestionSort { Votes, Recent }

public record SubmitQuestionRequest(string Content, bool IsAnonymous);

public record QuestionDto(
    Guid Id,
    string Content,
    bool IsAnonymous,
    string? AuthorUsername,
    string? AuthorDisplayName,
    int VoteCount,
    bool HasVoted,
    DateTime CreatedAt,
    bool IsVisible,
    AnswerDto? Answer,
    List<FollowUpDto> FollowUps
);

public record ReferencedAnswerDto(Guid AnswerId, Guid QuestionId, string QuestionContent, string AnswerContent);

public record AnswerDto(
    Guid Id, string Content, string? ImageUrl, string? YoutubeEmbedUrl,
    DateTime CreatedAt, ReferencedAnswerDto? ReferencedAnswer
);

public record SubmitAnswerRequest(string Content, string? ImageKey, string? YoutubeUrl, Guid? ReferencedAnswerId);

public record FollowUpDto(Guid Id, string Content, DateTime CreatedAt);
public record SubmitFollowUpRequest(string Content);

/// For the "reference a previous answer" picker when answering a new question.
public record MyAnswerSummaryDto(Guid AnswerId, Guid QuestionId, string QuestionContent, string AnswerContent);
