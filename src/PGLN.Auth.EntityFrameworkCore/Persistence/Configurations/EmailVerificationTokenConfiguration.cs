using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration
    : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(
        EntityTypeBuilder<EmailVerificationToken> builder)
    {
        var tokenIdConverter =
            new ValueConverter<EmailVerificationTokenId, Guid>(
                id => id.Value,
                value => new EmailVerificationTokenId(value));

        var userIdConverter =
            new ValueConverter<UserId, Guid>(
                id => id.Value,
                value => new UserId(value));

        builder.ToTable("EmailVerificationTokens");

        builder.HasKey(token => token.Id);

        builder
            .Property(token => token.Id)
            .HasConversion(tokenIdConverter)
            .ValueGeneratedNever();

        builder
            .Property(token => token.UserId)
            .HasConversion(userIdConverter)
            .IsRequired();

        builder
            .Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        builder
            .HasIndex(token => token.UserId);

        builder
            .Property(token => token.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(token => token.ExpiresAtUtc)
            .IsRequired();

        builder
            .Property(token => token.UsedAtUtc);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
