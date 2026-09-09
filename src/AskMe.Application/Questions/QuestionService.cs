using AskMe.Application.Common.Exceptions;
using AskMe.Application.Common.Interfaces;
using AskMe.Application.Common.Models;
using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Application.Questions;

public class QuestionService
{
    private readonly IApplicationDbContext _db;
    private readonly IRequestContextService _requestContext;
    private readonly IFileStorageService _storage;

    private const int MaxContentLength = 1000;
    private const int MaxFollowUpLength = 500;

    public QuestionService(IApplicationDbContext db, IRequestContextService requestContext, IFileStorageService storage)
    {
        _db = db;
        _requestContext = requestContext;
        _storage = storage;
    }

    // Strongly-typed projection shape shared by every query path below, so
    // MapToDto never has to guess at property names via `dynamic`. EF Core
    // can translate a constructor-based Select into this record directly.
    private sealed record QuestionProjection(
        Guid Id, string Content, bool IsAnonymous, int VoteCount, DateTime CreatedAt, bool IsVisible,
        string? AuthorUsername, string? AuthorDisplayName, bool HasVoted,
        Answer? Answer, Answer? ReferencedAnswer, Guid? ReferencedAnswerQuestionId,
        string? ReferencedAnswerQuestionContent, List<QuestionFollowUp> FollowUps);

    public async Task<Guid> SubmitQuestionAsync(
        string targetUsername, SubmitQuestionRequest request, Guid? authenticatedUserId, CancellationToken ct = default)
    {
        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            throw new ValidationException("Question content is required.");
        if (content.Length > MaxContentLength)
            throw new ValidationException($"Question content must be {MaxContentLength} characters or fewer.");

        var uname = targetUsername.Trim().ToLowerInvariant();
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == uname, ct)
            ?? throw new NotFoundException("User", targetUsername);

        var question = new Question
        {
            TargetUserId = target.Id,
            AuthorUserId = request.IsAnonymous ? null : authenticatedUserId,
            Content = content,
            IsAnonymous = request.IsAnonymous,
        };
        _db.Questions.Add(question);

        var security = new QuestionSecurityMetadata
        {
            QuestionId = question.Id,
            Question = question,
            SourceIp = _requestContext.IpAddress,
            UserAgent = _requestContext.UserAgent,
            AuthenticatedUserId = authenticatedUserId,
        };
        _db.QuestionSecurityMetadata.Add(security);

        await _db.SaveChangesAsync(ct);
        return question.Id;
    }

    public async Task<PagedResult<QuestionDto>> GetPublicFeedAsync(
        string username, int page, int pageSize, string? viewerIp, QuestionSort sort, string? search, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 20 : pageSize;

        var uname = username.Trim().ToLowerInvariant();
        var target = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == uname, ct)
            ?? throw new NotFoundException("User", username);

        var baseQuery = _db.Questions.AsNoTracking()
            .Where(q => q.TargetUserId == target.Id && q.IsVisible && !q.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            baseQuery = baseQuery.Where(q => q.Content.Contains(s) || (q.Answer != null && q.Answer.Content.Contains(s)));
        }

        var total = await baseQuery.CountAsync(ct);

        var sorted = sort == QuestionSort.Recent
            ? baseQuery.OrderByDescending(q => q.CreatedAt)
            : baseQuery.OrderByDescending(q => q.VoteCount).ThenByDescending(q => q.CreatedAt);

        var projected = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuestionProjection(
                q.Id, q.Content, q.IsAnonymous, q.VoteCount, q.CreatedAt, q.IsVisible,
                q.IsAnonymous ? null : (q.AuthorUserId != null ? q.AuthorUser!.Username : null),
                q.IsAnonymous ? null : (q.AuthorUserId != null ? q.AuthorUser!.DisplayName : null),
                viewerIp != null && q.Votes.Any(v => v.IpAddress == viewerIp),
                q.Answer,
                q.Answer != null ? q.Answer.ReferencedAnswer : null,
                q.Answer != null && q.Answer.ReferencedAnswer != null ? q.Answer.ReferencedAnswer.QuestionId : (Guid?)null,
                q.Answer != null && q.Answer.ReferencedAnswer != null ? q.Answer.ReferencedAnswer.Question!.Content : null,
                q.FollowUps.OrderBy(f => f.CreatedAt).ToList()))
            .ToListAsync(ct);

        var dtos = projected.Select(MapToDto).ToList();
        return new PagedResult<QuestionDto> { Items = dtos, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<QuestionDto> GetQuestionDetailAsync(Guid id, string? viewerIp, CancellationToken ct = default)
    {
        var q = await _db.Questions.AsNoTracking()
            .Include(x => x.Answer)!.ThenInclude(a => a!.ReferencedAnswer)!.ThenInclude(r => r!.Question)
            .Include(x => x.AuthorUser)
            .Include(x => x.Votes)
            .Include(x => x.FollowUps)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsVisible && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Question", id);

        var projection = new QuestionProjection(
            q.Id, q.Content, q.IsAnonymous, q.VoteCount, q.CreatedAt, q.IsVisible,
            q.IsAnonymous ? null : q.AuthorUser?.Username,
            q.IsAnonymous ? null : q.AuthorUser?.DisplayName,
            viewerIp != null && q.Votes.Any(v => v.IpAddress == viewerIp),
            q.Answer,
            q.Answer?.ReferencedAnswer,
            q.Answer?.ReferencedAnswer?.QuestionId,
            q.Answer?.ReferencedAnswer?.Question?.Content,
            q.FollowUps.OrderBy(f => f.CreatedAt).ToList());

        return MapToDto(projection);
    }

    public async Task<PagedResult<QuestionDto>> GetMyQuestionsAsync(
        Guid ownerId, int page, int pageSize, QuestionSort sort, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 20 : pageSize;

        var baseQuery = _db.Questions.AsNoTracking()
            .Where(q => q.TargetUserId == ownerId && !q.IsDeleted);

        var total = await baseQuery.CountAsync(ct);

        var sorted = sort == QuestionSort.Recent
            ? baseQuery.OrderByDescending(q => q.CreatedAt)
            : baseQuery.OrderByDescending(q => q.VoteCount).ThenByDescending(q => q.CreatedAt);

        var projected = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuestionProjection(
                q.Id, q.Content, q.IsAnonymous, q.VoteCount, q.CreatedAt, q.IsVisible,
                q.IsAnonymous ? null : (q.AuthorUserId != null ? q.AuthorUser!.Username : null),
                q.IsAnonymous ? null : (q.AuthorUserId != null ? q.AuthorUser!.DisplayName : null),
                false,
                q.Answer,
                q.Answer != null ? q.Answer.ReferencedAnswer : null,
                q.Answer != null && q.Answer.ReferencedAnswer != null ? q.Answer.ReferencedAnswer.QuestionId : (Guid?)null,
                q.Answer != null && q.Answer.ReferencedAnswer != null ? q.Answer.ReferencedAnswer.Question!.Content : null,
                q.FollowUps.OrderBy(f => f.CreatedAt).ToList()))
            .ToListAsync(ct);

        var dtos = projected.Select(MapToDto).ToList();
        return new PagedResult<QuestionDto> { Items = dtos, Page = page, PageSize = pageSize, TotalCount = total };
    }

    private async Task<Question> GetOwnedQuestionAsync(Guid questionId, Guid ownerId, CancellationToken ct)
    {
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == questionId, ct)
            ?? throw new NotFoundException("Question", questionId);

        if (q.TargetUserId != ownerId)
            throw new ForbiddenException("You can only manage questions sent to your own profile.");

        return q;
    }

    public async Task HideQuestionAsync(Guid questionId, Guid ownerId, CancellationToken ct = default)
    {
        var q = await GetOwnedQuestionAsync(questionId, ownerId, ct);
        q.IsVisible = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnhideQuestionAsync(Guid questionId, Guid ownerId, CancellationToken ct = default)
    {
        var q = await GetOwnedQuestionAsync(questionId, ownerId, ct);
        q.IsVisible = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteQuestionAsync(Guid questionId, Guid ownerId, CancellationToken ct = default)
    {
        var q = await GetOwnedQuestionAsync(questionId, ownerId, ct);
        q.IsDeleted = true;
        q.IsVisible = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AnswerQuestionAsync(Guid questionId, Guid ownerId, SubmitAnswerRequest request, CancellationToken ct = default)
    {
        var q = await _db.Questions.Include(x => x.Answer).FirstOrDefaultAsync(x => x.Id == questionId, ct)
            ?? throw new NotFoundException("Question", questionId);

        if (q.TargetUserId != ownerId)
            throw new ForbiddenException("You can only answer questions sent to your own profile.");

        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            throw new ValidationException("Answer content is required.");
        if (content.Length > MaxContentLength)
            throw new ValidationException($"Answer content must be {MaxContentLength} characters or fewer.");

        var youtubeUrl = NormalizeYoutubeUrl(request.YoutubeUrl);

        if (request.ReferencedAnswerId is { } refId)
        {
            var validReference = await _db.Answers.AsNoTracking()
                .AnyAsync(a => a.Id == refId && a.Question!.TargetUserId == ownerId, ct);
            if (!validReference)
                throw new ValidationException("You can only reference one of your own previous answers.");
        }

        if (q.Answer is null)
        {
            _db.Answers.Add(new Answer
            {
                QuestionId = q.Id,
                Content = content,
                ImageKey = request.ImageKey,
                YoutubeUrl = youtubeUrl,
                ReferencedAnswerId = request.ReferencedAnswerId,
            });
        }
        else
        {
            q.Answer.Content = content;
            q.Answer.ImageKey = request.ImageKey ?? q.Answer.ImageKey;
            q.Answer.YoutubeUrl = youtubeUrl ?? q.Answer.YoutubeUrl;
            q.Answer.ReferencedAnswerId = request.ReferencedAnswerId ?? q.Answer.ReferencedAnswerId;
            q.Answer.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MyAnswerSummaryDto>> GetMyAnswersAsync(Guid ownerId, string? search, CancellationToken ct = default)
    {
        var query = _db.Answers.AsNoTracking().Where(a => a.Question!.TargetUserId == ownerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a => a.Content.Contains(s) || a.Question!.Content.Contains(s));
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .Select(a => new MyAnswerSummaryDto(a.Id, a.QuestionId, a.Question!.Content, a.Content))
            .ToListAsync(ct);
    }

    public async Task<FollowUpDto> SubmitFollowUpAsync(Guid questionId, SubmitFollowUpRequest request, CancellationToken ct = default)
    {
        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            throw new ValidationException("Follow-up content is required.");
        if (content.Length > MaxFollowUpLength)
            throw new ValidationException($"Follow-up must be {MaxFollowUpLength} characters or fewer.");

        var exists = await _db.Questions.AnyAsync(q => q.Id == questionId && q.IsVisible && !q.IsDeleted, ct);
        if (!exists)
            throw new NotFoundException("Question", questionId);

        var followUp = new QuestionFollowUp { QuestionId = questionId, Content = content };
        _db.QuestionFollowUps.Add(followUp);
        await _db.SaveChangesAsync(ct);

        return new FollowUpDto(followUp.Id, followUp.Content, followUp.CreatedAt);
    }

    public async Task<string> UploadAnswerImageAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
        => await _storage.UploadAsync(content, fileName, contentType, ct);

    // ---------------- Mapping ----------------

    private QuestionDto MapToDto(QuestionProjection q)
    {
        var answer = q.Answer;

        AnswerDto? answerDto = answer is null ? null : new AnswerDto(
            answer.Id,
            answer.Content,
            answer.ImageKey is null ? null : _storage.GetPublicUrl(answer.ImageKey),
            ToYoutubeEmbedUrl(answer.YoutubeUrl),
            answer.CreatedAt,
            q.ReferencedAnswer is null ? null : new ReferencedAnswerDto(
                q.ReferencedAnswer.Id, q.ReferencedAnswerQuestionId!.Value,
                q.ReferencedAnswerQuestionContent!, q.ReferencedAnswer.Content)
        );

        var followUps = q.FollowUps.Select(f => new FollowUpDto(f.Id, f.Content, f.CreatedAt)).ToList();

        return new QuestionDto(
            q.Id, q.Content, q.IsAnonymous, q.AuthorUsername, q.AuthorDisplayName,
            q.VoteCount, q.HasVoted, q.CreatedAt, q.IsVisible, answerDto, followUps);
    }

    private static string? NormalizeYoutubeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        var isYoutube = trimmed.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
        if (!isYoutube)
            throw new ValidationException("That doesn't look like a YouTube link.");
        return trimmed;
    }

    /// Converts any common YouTube URL shape into an embeddable iframe src.
    private static string? ToYoutubeEmbedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        string? videoId = null;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                videoId = uri.AbsolutePath.Trim('/');
            else if (uri.AbsolutePath.Contains("/embed/", StringComparison.OrdinalIgnoreCase))
                videoId = uri.AbsolutePath.Split('/').Last();
            else
                videoId = GetQueryParam(uri.Query, "v");
        }

        return string.IsNullOrEmpty(videoId) ? null : $"https://www.youtube.com/embed/{videoId}";
    }

    // Deliberately not using System.Web.HttpUtility/Microsoft.AspNetCore.WebUtilities
    // here - neither is guaranteed available in a plain class library target,
    // and a YouTube "v" param is simple enough not to need either.
    private static string? GetQueryParam(string query, string key)
    {
        var q = query.TrimStart('?');
        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == key)
                return Uri.UnescapeDataString(parts[1]);
        }
        return null;
    }
}
