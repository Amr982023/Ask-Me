using AskMe.Application.Common.Interfaces;
using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskMe.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<QuestionVote> QuestionVotes => Set<QuestionVote>();
    public DbSet<QuestionSecurityMetadata> QuestionSecurityMetadata => Set<QuestionSecurityMetadata>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();
    public DbSet<BlockedIp> BlockedIps => Set<BlockedIp>();
    public DbSet<QuestionFollowUp> QuestionFollowUps => Set<QuestionFollowUp>();

    // Not exposed via IApplicationDbContext on purpose - image storage is an
    // Infrastructure concern (PostgresImageStorageService), never touched by
    // Application-layer use-case services.
    public DbSet<StoredImage> StoredImages => Set<StoredImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
