using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class BlockedIpConfiguration : IEntityTypeConfiguration<BlockedIp>
{
    public void Configure(EntityTypeBuilder<BlockedIp> b)
    {
        b.ToTable("BlockedIps");
        b.HasKey(x => x.Id);
        b.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(300);
        b.HasIndex(x => x.IpAddress).IsUnique();
    }
}
