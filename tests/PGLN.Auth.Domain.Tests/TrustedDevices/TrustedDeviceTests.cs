using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.TrustedDevices;

public sealed class TrustedDeviceTests
{
    private static readonly DateTimeOffset TrustedAtUtc =
        new(
            2026,
            9,
            12,
            10,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Create_ShouldCreateActiveTrustedDevice()
    {
        var userId =
            UserId.New();

        var trustedDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                userId,
                "device-hash-123",
                "Chrome on Windows",
                TrustedAtUtc);

        Assert.Equal(
            userId,
            trustedDevice.UserId);

        Assert.Equal(
            "device-hash-123",
            trustedDevice.DeviceIdHash);

        Assert.Equal(
            "Chrome on Windows",
            trustedDevice.DeviceName);

        Assert.Equal(
            TrustedAtUtc,
            trustedDevice.TrustedAtUtc);

        Assert.False(
            trustedDevice.IsRevoked);

        Assert.True(
            trustedDevice.IsTrusted);
    }

    [Fact]
    public void Create_WithBlankDeviceHash_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () =>
                TrustedDevice.Create(
                    TrustedDeviceId.New(),
                    UserId.New(),
                    " ",
                    "Test Device",
                    TrustedAtUtc));
    }

    [Fact]
    public void Revoke_ShouldRevokeTrustedDevice()
    {
        var trustedDevice =
            CreateTrustedDevice();

        var revokedAtUtc =
            TrustedAtUtc.AddHours(1);

        trustedDevice.Revoke(
            revokedAtUtc,
            "UserRevokedTrust");

        Assert.True(
            trustedDevice.IsRevoked);

        Assert.False(
            trustedDevice.IsTrusted);

        Assert.Equal(
            revokedAtUtc,
            trustedDevice.RevokedAtUtc);

        Assert.Equal(
            "UserRevokedTrust",
            trustedDevice.RevocationReason);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldRemainRevoked()
    {
        var trustedDevice =
            CreateTrustedDevice();

        trustedDevice.Revoke(
            TrustedAtUtc.AddHours(1),
            "FirstReason");

        trustedDevice.Revoke(
            TrustedAtUtc.AddHours(2),
            "SecondReason");

        Assert.Equal(
            TrustedAtUtc.AddHours(1),
            trustedDevice.RevokedAtUtc);

        Assert.Equal(
            "FirstReason",
            trustedDevice.RevocationReason);
    }

    [Fact]
    public void TrustAgain_WhenRevoked_ShouldRestoreTrust()
    {
        var trustedDevice =
            CreateTrustedDevice();

        trustedDevice.Revoke(
            TrustedAtUtc.AddHours(1),
            "UserRevokedTrust");

        var trustedAgainAtUtc =
            TrustedAtUtc.AddHours(2);

        trustedDevice.TrustAgain(
            trustedAgainAtUtc,
            "Updated Device Name");

        Assert.True(
            trustedDevice.IsTrusted);

        Assert.False(
            trustedDevice.IsRevoked);

        Assert.Equal(
            trustedAgainAtUtc,
            trustedDevice.TrustedAtUtc);

        Assert.Equal(
            "Updated Device Name",
            trustedDevice.DeviceName);

        Assert.Null(
            trustedDevice.RevokedAtUtc);

        Assert.Null(
            trustedDevice.RevocationReason);
    }

    [Fact]
    public void Revoke_BeforeTrustedAtUtc_ShouldThrow()
    {
        var trustedDevice =
            CreateTrustedDevice();

        Assert.Throws<ArgumentException>(
            () =>
                trustedDevice.Revoke(
                    TrustedAtUtc.AddMinutes(-1),
                    "Invalid"));
    }

    private static TrustedDevice CreateTrustedDevice()
    {
        return TrustedDevice.Create(
            TrustedDeviceId.New(),
            UserId.New(),
            "device-hash-123",
            "Test Device",
            TrustedAtUtc);
    }
}

