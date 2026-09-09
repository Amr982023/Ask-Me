namespace AskMe.Application.Admin;

public record AdminQuestionDto(
    Guid Id,
    string TargetUsername,
    string Content,
    bool IsAnonymous,
    bool IsVisible,
    bool IsDeleted,
    int VoteCount,
    DateTime CreatedAt
);

public record SecurityMetadataDto(
    Guid QuestionId,
    string SourceIp,
    string? UserAgent,
    Guid? AuthenticatedUserId,
    DateTime SubmittedAt
);

public record PlatformStatsDto(
    int TotalUsers,
    int TotalQuestions,
    int TotalAnswers,
    int HiddenQuestions,
    int QuestionsLast7Days
);

public record AdminUserDto(
    Guid Id, string Username, string DisplayName, string Email,
    string Role, int QuestionCount, DateTime CreatedAt, string? RegistrationIp
);

public record BlockedIpDto(Guid Id, string IpAddress, string? Reason, DateTime CreatedAt);
public record BlockIpRequest(string IpAddress, string? Reason);
