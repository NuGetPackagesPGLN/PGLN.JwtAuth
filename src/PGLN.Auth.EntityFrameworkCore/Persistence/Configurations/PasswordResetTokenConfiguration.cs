using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration
    : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(
        EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable(
            "PasswordResetTokens");

        builder.HasKey(
            token =>
                token.Id);

        builder.Property(
                token =>
                    token.Id)
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    new PasswordResetTokenId(
                        value));

        builder.Property(
                token =>
                    token.UserId)
            .HasConversion(
                userId =>
                    userId.Value,
                value =>
                    new UserId(
                        value))
            .IsRequired();

        builder.Property(
                token =>
                    token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(
                token =>
                    token.CreatedAtUtc)
            .HasConversion(
                value =>
                    value.UtcDateTime,
                value =>
                    new DateTimeOffset(
                        DateTime.SpecifyKind(
                            value,
                            DateTimeKind.Utc)))
            .IsRequired();

        builder.Property(
                token =>
                    token.ExpiresAtUtc)
            .HasConversion(
                value =>
                    value.UtcDateTime,
                value =>
                    new DateTimeOffset(
                        DateTime.SpecifyKind(
                            value,
                            DateTimeKind.Utc)))
            .IsRequired();

        builder.Property(
                token =>
                    token.UsedAtUtc)
            .HasConversion(
                value =>
                    value.HasValue
                        ? value.Value.UtcDateTime
                        : (DateTime?)null,
                value =>
                    value.HasValue
                        ? new DateTimeOffset(
                            DateTime.SpecifyKind(
                                value.Value,
                                DateTimeKind.Utc))
                        : null);

        builder.HasIndex(
                token =>
                    token.TokenHash)
            .IsUnique();

        builder.HasIndex(
            token =>
                token.UserId);

        builder.HasIndex(
            token =>
                token.ExpiresAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                token =>
                    token.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
