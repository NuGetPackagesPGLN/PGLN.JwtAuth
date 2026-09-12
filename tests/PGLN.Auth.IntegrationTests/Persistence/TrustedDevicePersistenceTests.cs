using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class TrustedDevicePersistenceTests
{
    [Fact]
    public async Task TrustedDevice_ShouldPersistAndReload()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        await using var dbContext =
            new AuthDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        var now =
            new DateTimeOffset(
                2026,
                9,
                12,
                12,
                0,
                0,
                TimeSpan.Zero);

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "trusted-device@example.com"),
                "hashed-password",
                now.AddDays(-1));

        user.ClearDomainEvents();

        var trustedDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-123",
                "Chrome on Windows",
                now);

        dbContext.Users.Add(
            user);

        dbContext.TrustedDevices.Add(
            trustedDevice);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persistedDevice =
            await dbContext.TrustedDevices
                .SingleAsync(
                    device =>
                        device.Id ==
                        trustedDevice.Id);

        Assert.Equal(
            trustedDevice.Id,
            persistedDevice.Id);

        Assert.Equal(
            user.Id,
            persistedDevice.UserId);

        Assert.Equal(
            "device-hash-123",
            persistedDevice.DeviceIdHash);

        Assert.Equal(
            "Chrome on Windows",
            persistedDevice.DeviceName);

        Assert.Equal(
            now.ToUnixTimeMilliseconds(),
            persistedDevice.TrustedAtUtc
                .ToUnixTimeMilliseconds());

        Assert.False(
            persistedDevice.IsRevoked);

        Assert.True(
            persistedDevice.IsTrusted);
    }

    [Fact]
    public async Task RevokedTrustedDevice_ShouldPersistAndReload()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        await using var dbContext =
            new AuthDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        var trustedAtUtc =
            new DateTimeOffset(
                2026,
                9,
                12,
                12,
                0,
                0,
                TimeSpan.Zero);

        var revokedAtUtc =
            trustedAtUtc.AddHours(1);

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "revoked-device@example.com"),
                "hashed-password",
                trustedAtUtc.AddDays(-1));

        user.ClearDomainEvents();

        var trustedDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "revoked-device-hash",
                "Firefox on Linux",
                trustedAtUtc);

        trustedDevice.Revoke(
            revokedAtUtc,
            "UserRevokedTrust");

        dbContext.Users.Add(
            user);

        dbContext.TrustedDevices.Add(
            trustedDevice);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persistedDevice =
            await dbContext.TrustedDevices
                .SingleAsync(
                    device =>
                        device.Id ==
                        trustedDevice.Id);

        Assert.True(
            persistedDevice.IsRevoked);

        Assert.False(
            persistedDevice.IsTrusted);

        Assert.Equal(
            revokedAtUtc.ToUnixTimeMilliseconds(),
            persistedDevice.RevokedAtUtc!
                .Value
                .ToUnixTimeMilliseconds());

        Assert.Equal(
            "UserRevokedTrust",
            persistedDevice.RevocationReason);
    }
}
