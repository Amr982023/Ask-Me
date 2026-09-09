using AskMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMe.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);

        b.Property(u => u.Username).HasMaxLength(30).IsRequired();
        b.HasIndex(u => u.Username).IsUnique(); // powers /u/{username} lookup

        b.Property(u => u.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();

        // Nullable now: Google/Facebook-only accounts have no local password.
        b.Property(u => u.PasswordHash);
        b.Property(u => u.DisplayName).HasMaxLength(60);
        b.Property(u => u.Bio).HasMaxLength(300);
        b.Property(u => u.AboutMe).HasMaxLength(2000);
        b.Property(u => u.WebsiteUrl).HasMaxLength(300);
        b.Property(u => u.TwitterUrl).HasMaxLength(300);
        b.Property(u => u.FacebookUrl).HasMaxLength(300);
        b.Property(u => u.GithubUrl).HasMaxLength(300);
        b.Property(u => u.LinkedinUrl).HasMaxLength(300);
        b.Property(u => u.YoutubeUrl).HasMaxLength(300);
        b.Property(u => u.TiktokUrl).HasMaxLength(300);
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        b.Property(u => u.ExternalProvider).HasMaxLength(20);
        b.Property(u => u.ExternalId).HasMaxLength(200);
        b.Property(u => u.RegistrationIp).HasMaxLength(45);

        // Lookup used when linking/finding an account by its Google/Facebook identity.
        b.HasIndex(u => new { u.ExternalProvider, u.ExternalId });

        b.HasMany(u => u.QuestionsReceived)
            .WithOne(q => q.TargetUser)
            .HasForeignKey(q => q.TargetUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(u => u.QuestionsAuthored)
            .WithOne(q => q.AuthorUser)
            .HasForeignKey(q => q.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
