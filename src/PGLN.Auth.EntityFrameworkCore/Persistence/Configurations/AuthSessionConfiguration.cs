using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class AuthSessionConfiguration
    : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(
        EntityTypeBuilder<AuthSession> builder)
    {
        var sessionIdConverter =
            new ValueConverter<AuthSessionId, Guid>(
                id =>
                    id.Value,
                value =>
                    new AuthSessionId(value));

        var userIdConverter =
            new ValueConverter<UserId, Guid>(
                id =>
                    id.Value,
                value =>
                    new UserId(value));

        var dateTimeOffsetConverter =
            new ValueConverter<DateTimeOffset, long>(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset.FromUnixTimeMilliseconds(
                        value));

        var nullableDateTimeOffsetConverter =
            new ValueConverter<DateTimeOffset?, long?>(
                value =>
                    value.HasValue
                        ? value.Value.ToUnixTimeMilliseconds()
                        : null,
                value =>
                    value.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(
                            value.Value)
                        : null);

        builder.ToTable(
            "AuthSessions");

        builder.HasKey(
            session =>
                session.Id);

        builder
            .Property(
                session =>
                    session.Id)
            .HasConversion(
                sessionIdConverter)
            .ValueGeneratedNever();

        builder
            .Property(
                session =>
                    session.UserId)
            .HasConversion(
                userIdConverter)
            .IsRequired();

        builder
            .Property(
                session =>
                    session.DeviceIdHash)
            .HasMaxLength(
                256)
            .IsRequired();

        builder
            .Property(
                session =>
                    session.DeviceName)
            .HasMaxLength(
                256);

        builder
            .Property(
                session =>
                    session.IpAddress)
            .HasMaxLength(
                64);

        builder
            .Property(
                session =>
                    session.UserAgent)
            .HasMaxLength(
                1024);

        builder
            .Property(
                session =>
                    session.CreatedAtUtc)
            .HasConversion(
                dateTimeOffsetConverter)
            .IsRequired();

        builder
            .Property(
                session =>
                    session.LastSeenAtUtc)
            .HasConversion(
                dateTimeOffsetConverter)
            .IsRequired();

        builder
            .Property(
                session =>
                    session.RevokedAtUtc)
            .HasConversion(
                nullableDateTimeOffsetConverter);

        builder
            .Property(
                session =>
                    session.RevocationReason)
            .HasMaxLength(
                256);

        builder
            .Property(
                session =>
                    session.DeviceTrustStatus)
            .HasConversion<int>()
            .IsRequired();

        builder
            .HasIndex(
                session =>
                    session.UserId);

        builder
            .HasIndex(
                session =>
                    new
                    {
                        session.UserId,
                        session.DeviceIdHash
                    });

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(
                session =>
                    session.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.Ignore(
            session =>
                session.IsRevoked);

        builder.Ignore(
            session =>
                session.IsActive);

        builder.Ignore(
            session =>
                session.IsTrustedDevice);
    }
}

