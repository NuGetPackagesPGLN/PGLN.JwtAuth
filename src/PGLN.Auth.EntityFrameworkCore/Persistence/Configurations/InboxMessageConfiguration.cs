using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.EntityFrameworkCore.Inbox;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class InboxMessageConfiguration
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(
        EntityTypeBuilder<InboxMessage> builder)
    {
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

        builder.ToTable("InboxMessages");

        builder.HasKey(message => message.Id);

        builder
            .Property(message => message.Id)
            .ValueGeneratedNever();

        builder
            .Property(message => message.ProcessedAtUtc)
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
            .HasIndex(message => message.ProcessedAtUtc);

        builder
            .HasIndex(message => message.ClaimExpiresAtUtc);
    }
}
