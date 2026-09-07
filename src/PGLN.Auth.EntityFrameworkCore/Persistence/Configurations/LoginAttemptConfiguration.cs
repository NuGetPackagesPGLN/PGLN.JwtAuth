using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class LoginAttemptConfiguration
    : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(
        EntityTypeBuilder<LoginAttempt> builder)
    {
        var idConverter =
            new ValueConverter<LoginAttemptId, Guid>(
                id => id.Value,
                value => new LoginAttemptId(value));

        var userIdConverter =
            new ValueConverter<UserId?, Guid?>(
                id =>
                    id.HasValue
                        ? id.Value.Value
                        : null,
                value =>
                    value.HasValue
                        ? new UserId(value.Value)
                        : null);

        builder.ToTable(
            "LoginAttempts");

        builder.HasKey(
            attempt => attempt.Id);

        builder
            .Property(attempt => attempt.Id)
            .HasConversion(idConverter)
            .ValueGeneratedNever();

        builder
            .Property(attempt => attempt.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder
            .Property(attempt => attempt.UserId)
            .HasConversion(userIdConverter);

        builder
            .Property(attempt => attempt.Succeeded)
            .IsRequired();

        builder
            .Property(attempt => attempt.FailureReason)
            .HasConversion<int?>();

        builder
            .Property(attempt => attempt.AttemptedAtUtc)
            .IsRequired();

        builder
            .HasIndex(attempt => attempt.Email);

        builder
            .HasIndex(attempt => attempt.UserId);

        builder
            .HasIndex(attempt => attempt.AttemptedAtUtc);

        builder
            .HasIndex(
                attempt =>
                    new
                    {
                        attempt.Email,
                        attempt.AttemptedAtUtc
                    });

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(attempt => attempt.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
