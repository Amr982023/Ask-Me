using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskMe.Infrastructure.Persistence.Configurations
{
    public class StoredImageConfiguration : IEntityTypeConfiguration<StoredImage>
    {
        public void Configure(EntityTypeBuilder<StoredImage> b)
        {
            b.ToTable("StoredImages");
            b.HasKey(i => i.Id);

            // Raw binary column - Npgsql maps byte[] to bytea. No Base64, no JSON.
            b.Property(i => i.Data).HasColumnType("bytea").IsRequired();
            b.Property(i => i.ContentType).HasMaxLength(50).IsRequired();
        }
    }
}
