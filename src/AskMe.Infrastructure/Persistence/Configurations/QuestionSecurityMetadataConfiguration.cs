using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class QuestionSecurityMetadataConfiguration : IEntityTypeConfiguration<QuestionSecurityMetadata>
{
    public void Configure(EntityTypeBuilder<QuestionSecurityMetadata> b)
    {
        b.ToTable("QuestionSecurityMetadata");
        b.HasKey(m => m.Id);
        b.Property(m => m.SourceIp).HasMaxLength(45).IsRequired();
        b.Property(m => m.UserAgent).HasMaxLength(500);
        b.HasIndex(m => m.QuestionId).IsUnique();

        // No navigation/DTO in Application ever exposes this table publicly -
        // that boundary is enforced by never mapping it in Questions/QuestionDtos.
    }
}
