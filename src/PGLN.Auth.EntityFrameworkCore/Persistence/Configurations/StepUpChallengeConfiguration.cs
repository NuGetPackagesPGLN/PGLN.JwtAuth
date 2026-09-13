using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLN.Auth.Domain.StepUpChallenges;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class StepUpChallengeConfiguration
    : IEntityTypeConfiguration<StepUpChallenge>
{
    public void Configure(
        EntityTypeBuilder<StepUpChallenge> builder)
    {
        builder.ToTable(
            "StepUpChallenges");

        builder.HasKey(
            challenge =>
                challenge.Id);

        builder
            .Property(
                challenge =>
                    challenge.Id)
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    new StepUpChallengeId(
                        value));

        builder
            .Property(
                challenge =>
                    challenge.UserId)
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    new Domain.Users.UserId(
                        value))
            .IsRequired();

        builder
            .Property(
                challenge =>
                    challenge.DeviceIdHash)
            .HasMaxLength(
                512)
            .IsRequired();

        builder
            .Property(
                challenge =>
                    challenge.DeviceName)
            .HasMaxLength(
                256);

        builder
            .Property(
                challenge =>
                    challenge.CodeHash)
            .HasMaxLength(
                128)
            .IsRequired();

        builder
            .Property(
                challenge =>
                    challenge.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(
                challenge =>
                    challenge.ExpiresAtUtc)
            .IsRequired();

        builder
            .Property(
                challenge =>
                    challenge.VerifiedAtUtc);

        builder
            .Property(
                challenge =>
                    challenge.FailedAttempts)
            .IsRequired();

        builder.Ignore(
            challenge =>
                challenge.IsVerified);

        builder.HasIndex(
            challenge =>
                challenge.UserId);

        builder.HasIndex(
            challenge =>
                new
                {
                    challenge.UserId,
                    challenge.DeviceIdHash
                });

        builder
            .HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(
                challenge =>
                    challenge.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}

