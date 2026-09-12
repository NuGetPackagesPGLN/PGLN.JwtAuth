using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class EmailChangeTokenConfiguration
    : IEntityTypeConfiguration<EmailChangeToken>
{
    public void Configure(
        EntityTypeBuilder<EmailChangeToken> builder)
    {
        var tokenIdConverter =
            new ValueConverter<EmailChangeTokenId, Guid>(
                id => id.Value,
                value => new EmailChangeTokenId(value));

        var userIdConverter =
            new ValueConverter<UserId, Guid>(
                id => id.Value,
                value => new UserId(value));

        builder.ToTable("EmailChangeTokens");

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
            .Property(token => token.NewEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder
            .Property(token => token.NormalizedNewEmail)
            .HasMaxLength(320)
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
            .HasIndex(token => token.NormalizedNewEmail);

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
