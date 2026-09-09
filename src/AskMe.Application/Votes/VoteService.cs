using AskMe.Application.Common.Exceptions;
using AskMe.Application.Common.Interfaces;
using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Application.Votes;

public class VoteService
{
    private readonly IApplicationDbContext _db;
    private readonly IRequestContextService _requestContext;

    public VoteService(IApplicationDbContext db, IRequestContextService requestContext)
    {
        _db = db;
        _requestContext = requestContext;
    }

    /// Casts an upvote. The application does an existence pre-check purely to
    /// return a friendlier error quickly, but the UNIQUE(QuestionId, IpAddress)
    /// database constraint (see QuestionVoteConfiguration) is the actual,
    /// final protection against duplicates under concurrent requests - a
    /// unique-violation from SaveChangesAsync is caught and translated below.
    public async Task<int> VoteAsync(Guid questionId, CancellationToken ct = default)
    {
        var ip = _requestContext.IpAddress;

        var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == questionId && q.IsVisible && !q.IsDeleted, ct)
            ?? throw new NotFoundException("Question", questionId);

        var alreadyVoted = await _db.QuestionVotes.AnyAsync(v => v.QuestionId == questionId && v.IpAddress == ip, ct);
        if (alreadyVoted)
            throw new DuplicateVoteException();

        _db.QuestionVotes.Add(new QuestionVote { QuestionId = questionId, IpAddress = ip });
        question.VoteCount += 1;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // A concurrent request beat us to it between the check above and
            // the insert - the DB constraint is what actually caught it.
            throw new DuplicateVoteException();
        }

        return question.VoteCount;
    }

    /// Removes the current IP's upvote from a question, if one exists.
    /// Idempotent - unvoting when there's no existing vote is a no-op rather
    /// than an error, since the frontend may retry/race a click.
    public async Task<int> UnvoteAsync(Guid questionId, CancellationToken ct = default)
    {
        var ip = _requestContext.IpAddress;

        var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == questionId && q.IsVisible && !q.IsDeleted, ct)
            ?? throw new NotFoundException("Question", questionId);

        var vote = await _db.QuestionVotes.FirstOrDefaultAsync(v => v.QuestionId == questionId && v.IpAddress == ip, ct);
        if (vote is null)
            return question.VoteCount;

        _db.QuestionVotes.Remove(vote);
        question.VoteCount = Math.Max(0, question.VoteCount - 1);
        await _db.SaveChangesAsync(ct);

        return question.VoteCount;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Postgres unique_violation SQLSTATE is 23505; Npgsql surfaces this on
        // the inner exception. Kept string-based here so Application doesn't
        // need to reference the Npgsql package directly.
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("23505") || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
