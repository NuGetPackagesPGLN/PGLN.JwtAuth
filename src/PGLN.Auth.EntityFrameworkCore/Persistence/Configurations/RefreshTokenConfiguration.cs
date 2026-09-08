using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(
            "RefreshTokens");

        builder.HasKey(
            refreshToken =>
                refreshToken.Id);

        builder.Property(
                refreshToken =>
                    refreshToken.Id)
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    new RefreshTokenId(
                        value));

        builder.Property(
                refreshToken =>
                    refreshToken.FamilyId)
            .HasConversion(
                familyId =>
                    familyId.Value,
                value =>
                    RefreshTokenFamilyId.From(
                        value))
            .IsRequired();

        builder.Property(
                refreshToken =>
                    refreshToken.UserId)
            .HasConversion(
                userId =>
                    userId.Value,
                value =>
                    new UserId(
                        value))
            .IsRequired();

        builder.Property(
                refreshToken =>
                    refreshToken.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(
                refreshToken =>
                    refreshToken.CreatedAtUtc)
            .IsRequired();

        builder.Property(
                refreshToken =>
                    refreshToken.ExpiresAtUtc)
            .IsRequired();

        builder.Property(
            refreshToken =>
                refreshToken.RevokedAtUtc);

        builder.Property(
                refreshToken =>
                    refreshToken.RevocationReason)
            .HasMaxLength(256);

        builder.Property(
                refreshToken =>
                    refreshToken.ReplacedByTokenId)
            .HasConversion(
                replacementId =>
                    replacementId.HasValue
                        ? replacementId.Value.Value
                        : (Guid?)null,
                value =>
                    value.HasValue
                        ? new RefreshTokenId(
                            value.Value)
                        : null);

        builder.HasIndex(
                refreshToken =>
                    refreshToken.TokenHash)
            .IsUnique();

        builder.HasIndex(
            refreshToken =>
                refreshToken.UserId);

        builder.HasIndex(
            refreshToken =>
                refreshToken.FamilyId);

        builder.HasIndex(
            refreshToken =>
                refreshToken.ExpiresAtUtc);
    }
}
