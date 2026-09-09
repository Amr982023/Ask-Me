using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> b)
    {
        b.ToTable("PendingRegistrations");
        b.HasKey(p => p.Id);

        b.Property(p => p.Username).HasMaxLength(30).IsRequired();
        b.Property(p => p.Email).HasMaxLength(256).IsRequired();
        b.Property(p => p.PasswordHash).IsRequired();
        b.Property(p => p.DisplayName).HasMaxLength(60);
        b.Property(p => p.RegistrationIp).HasMaxLength(45);
        b.Property(p => p.CodeHash).IsRequired();

        // One in-flight registration per email - a repeat registration
        // attempt with the same email overwrites (not duplicates) the
        // pending row, which is also how "resend" effectively works for
        // this flow.
        b.HasIndex(p => p.Email).IsUnique();
    }
}
