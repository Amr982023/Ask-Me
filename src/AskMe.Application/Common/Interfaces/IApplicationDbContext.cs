using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Application.Common.Interfaces;

// Abstraction over EF Core's DbContext so Application never references
// Infrastructure/EF Core directly - keeps Infrastructure swappable.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Question> Questions { get; }
    DbSet<Answer> Answers { get; }
    DbSet<QuestionVote> QuestionVotes { get; }
    DbSet<QuestionSecurityMetadata> QuestionSecurityMetadata { get; }
    DbSet<SecurityAuditLog> SecurityAuditLogs { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<PendingRegistration> PendingRegistrations { get; }
    DbSet<BlockedIp> BlockedIps { get; }
    DbSet<QuestionFollowUp> QuestionFollowUps { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
