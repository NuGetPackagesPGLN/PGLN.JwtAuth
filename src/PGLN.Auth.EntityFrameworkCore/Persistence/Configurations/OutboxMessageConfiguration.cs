using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.EntityFrameworkCore.Outbox;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        var dateTimeOffsetConverter =
            new ValueConverter<DateTimeOffset, long>(
                value =>
                    value.ToUnixTimeMilliseconds(),
                value =>
                    DateTimeOffset.FromUnixTimeMilliseconds(value));

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

        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder
            .Property(message => message.Id)
            .ValueGeneratedNever();

        builder
            .Property(message => message.Type)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(message => message.Payload)
            .IsRequired();

        builder
            .Property(message => message.OccurredAtUtc)
            .HasConversion(dateTimeOffsetConverter)
            .IsRequired();

        builder
            .Property(message => message.ProcessedAtUtc)
            .HasConversion(nullableDateTimeOffsetConverter);

        builder
            .Property(message => message.NextAttemptAtUtc)
            .HasConversion(nullableDateTimeOffsetConverter);

        builder
            .Property(message => message.DeadLetteredAtUtc)
            .HasConversion(nullableDateTimeOffsetConverter);

        builder
            .Property(message => message.ClaimedAtUtc)
            .HasConversion(nullableDateTimeOffsetConverter);

        builder
            .Property(message => message.ClaimExpiresAtUtc)
            .HasConversion(nullableDateTimeOffsetConverter);

        builder
            .Property(message => message.ClaimedBy)
            .HasMaxLength(256);

        builder
            .Property(message => message.AttemptCount)
            .IsRequired();

        builder
            .Property(message => message.LastError)
            .HasMaxLength(4000);

        builder
            .HasIndex(message => message.ProcessedAtUtc);

        builder
            .HasIndex(message => message.NextAttemptAtUtc);

        builder
            .HasIndex(message => message.DeadLetteredAtUtc);

        builder
            .HasIndex(message => message.ClaimExpiresAtUtc);

        builder
            .HasIndex(message => message.OccurredAtUtc);
    }
}
