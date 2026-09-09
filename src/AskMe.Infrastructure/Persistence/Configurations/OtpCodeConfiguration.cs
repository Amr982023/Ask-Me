using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> b)
    {
        b.ToTable("OtpCodes");
        b.HasKey(o => o.Id);
        b.Property(o => o.CodeHash).IsRequired();
        b.Property(o => o.Purpose).HasConversion<string>().HasMaxLength(30);

        // Powers "find the latest unconsumed, unexpired code for this
        // user+purpose" in AuthService.ConsumeOtpOrThrowAsync.
        b.HasIndex(o => new { o.UserId, o.Purpose, o.ConsumedAt, o.ExpiresAt });

        b.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
