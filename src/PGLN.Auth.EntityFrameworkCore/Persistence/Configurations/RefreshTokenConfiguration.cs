using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder)
    {
        var idConverter =
            new ValueConverter<RefreshTokenId, Guid>(
                id => id.Value,
                value => new RefreshTokenId(value));

        var userIdConverter =
            new ValueConverter<UserId, Guid>(
                id => id.Value,
                value => new UserId(value));

        var replacementConverter =
            new ValueConverter<RefreshTokenId?, Guid?>(
                id =>
                    id.HasValue
                        ? id.Value.Value
                        : null,
                value =>
                    value.HasValue
                        ? new RefreshTokenId(value.Value)
                        : null);

        builder.ToTable(
            "RefreshTokens");

        builder.HasKey(
            token => token.Id);

        builder
            .Property(token => token.Id)
            .HasConversion(idConverter)
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
            .HasIndex(token => token.ExpiresAtUtc);

        builder
            .Property(token => token.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(token => token.ExpiresAtUtc)
            .IsRequired();

        builder
            .Property(token => token.RevokedAtUtc);

        builder
            .Property(token => token.RevocationReason)
            .HasMaxLength(256);

        builder
            .Property(token => token.ReplacedByTokenId)
            .HasConversion(replacementConverter);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
