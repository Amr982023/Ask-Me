using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class QuestionVoteConfiguration : IEntityTypeConfiguration<QuestionVote>
{
    public void Configure(EntityTypeBuilder<QuestionVote> b)
    {
        b.ToTable("QuestionVotes");
        b.HasKey(v => v.Id);

        // Postgres 'inet' would be ideal, but plain varchar keeps this portable
        // and is what EF Core maps to reliably without extra config; the
        // uniqueness guarantee below is what actually matters for correctness.
        b.Property(v => v.IpAddress).HasMaxLength(45).IsRequired();

        // THE authoritative anti-duplicate-vote guard - enforced by Postgres,
        // not just application logic. A violation surfaces as a 23505 error
        // that VoteService translates into a DuplicateVoteException.
        b.HasIndex(v => new { v.QuestionId, v.IpAddress })
            .IsUnique()
            .HasDatabaseName("UX_QuestionVotes_QuestionId_IpAddress");
    }
}
