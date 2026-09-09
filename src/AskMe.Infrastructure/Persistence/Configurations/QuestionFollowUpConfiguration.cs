using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class QuestionFollowUpConfiguration : IEntityTypeConfiguration<QuestionFollowUp>
{
    public void Configure(EntityTypeBuilder<QuestionFollowUp> b)
    {
        b.ToTable("QuestionFollowUps");
        b.HasKey(x => x.Id);
        b.Property(x => x.Content).HasMaxLength(500).IsRequired();
        b.HasIndex(x => x.QuestionId);

        b.HasOne(x => x.Question)
            .WithMany(q => q.FollowUps)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
