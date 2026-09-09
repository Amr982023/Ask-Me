using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> b)
    {
        b.ToTable("Answers");
        b.HasKey(a => a.Id);
        b.Property(a => a.Content).HasMaxLength(1000).IsRequired();
        b.Property(a => a.YoutubeUrl).HasMaxLength(300);
        b.HasIndex(a => a.QuestionId).IsUnique(); // one answer per question

        // Restrict (not Cascade) - deleting an answer that another answer
        // references should fail rather than silently null out someone
        // else's reference. Self-referencing FK, so EF needs this spelled
        // out explicitly to avoid a multiple-cascade-path error anyway.
        b.HasOne(a => a.ReferencedAnswer)
            .WithMany()
            .HasForeignKey(a => a.ReferencedAnswerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
