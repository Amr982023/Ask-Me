using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> b)
    {
        b.ToTable("Questions");
        b.HasKey(q => q.Id);
        b.Property(q => q.Content).HasMaxLength(1000).IsRequired();

        // This composite index lets the public/owner feed query
        // (WHERE TargetUserId = ? AND IsVisible ORDER BY VoteCount DESC, CreatedAt DESC)
        // be served directly off the index, with no in-memory sort.
        b.HasIndex(q => new { q.TargetUserId, q.IsVisible, q.VoteCount, q.CreatedAt })
            .HasDatabaseName("IX_Questions_Feed");

        b.HasIndex(q => q.CreatedAt);
        b.HasIndex(q => q.TargetUserId);

        b.HasOne(q => q.Answer)
            .WithOne(a => a.Question)
            .HasForeignKey<Answer>(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(q => q.SecurityMetadata)
            .WithOne(m => m.Question)
            .HasForeignKey<QuestionSecurityMetadata>(m => m.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(q => q.Votes)
            .WithOne(v => v.Question)
            .HasForeignKey(v => v.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
