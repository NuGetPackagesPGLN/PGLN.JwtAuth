using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLN.Auth.Domain.TrustedDevices;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class TrustedDeviceConfiguration
    : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(
        EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.ToTable(
            "TrustedDevices");

        builder.HasKey(
            device =>
                device.Id);

        builder
            .Property(
                device =>
                    device.Id)
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    new TrustedDeviceId(value))
            .ValueGeneratedNever();

        builder
            .Property(
                device =>
                    device.UserId)
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    new(value))
            .IsRequired();

        builder
            .Property(
                device =>
                    device.DeviceIdHash)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(
                device =>
                    device.DeviceName)
            .HasMaxLength(256);

        builder
            .Property(
                device =>
                    device.TrustedAtUtc)
            .IsRequired();

        builder
            .Property(
                device =>
                    device.RevokedAtUtc);

        builder
            .Property(
                device =>
                    device.RevocationReason)
            .HasMaxLength(256);

        builder.Ignore(
            device =>
                device.IsRevoked);

        builder.Ignore(
            device =>
                device.IsTrusted);

        builder.HasIndex(
                device =>
                    new
                    {
                        device.UserId,
                        device.DeviceIdHash
                    })
            .IsUnique();

        builder
            .HasOne<PGLN.Auth.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(
                device =>
                    device.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
