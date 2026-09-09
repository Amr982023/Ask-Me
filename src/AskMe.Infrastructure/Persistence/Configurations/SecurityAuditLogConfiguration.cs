using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> b)
    {
        b.ToTable("SecurityAuditLogs");
        b.HasKey(l => l.Id);
        b.Property(l => l.Action).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(l => l.QuestionId);
        b.HasIndex(l => l.AdminUserId);
    }
}
