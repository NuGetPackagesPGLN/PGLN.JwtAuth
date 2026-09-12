using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.Sessions;

public sealed class AuthSessionDeviceTrustTests
{
    [Fact]
    public void Create_ShouldDefaultDeviceTrustStatusToUnknown()
    {
        var session =
            CreateSession();

        Assert.Equal(
            DeviceTrustStatus.Unknown,
            session.DeviceTrustStatus);

        Assert.False(
            session.IsTrustedDevice);
    }

    [Fact]
    public void TrustDevice_ShouldMarkDeviceAsTrusted()
    {
        var session =
            CreateSession();

        session.TrustDevice();

        Assert.Equal(
            DeviceTrustStatus.Trusted,
            session.DeviceTrustStatus);

        Assert.True(
            session.IsTrustedDevice);
    }

    [Fact]
    public void RevokeDeviceTrust_ShouldMarkDeviceTrustAsRevoked()
    {
        var session =
            CreateSession();

        session.TrustDevice();

        session.RevokeDeviceTrust();

        Assert.Equal(
            DeviceTrustStatus.Revoked,
            session.DeviceTrustStatus);

        Assert.False(
            session.IsTrustedDevice);
    }

    [Fact]
    public void TrustDevice_WhenSessionIsRevoked_ShouldNotTrustDevice()
    {
        var createdAtUtc =
            DateTimeOffset.UtcNow;

        var session =
            CreateSession(
                createdAtUtc);

        session.Revoke(
            createdAtUtc.AddMinutes(1),
            "User signed out.");

        session.TrustDevice();

        Assert.Equal(
            DeviceTrustStatus.Unknown,
            session.DeviceTrustStatus);

        Assert.False(
            session.IsTrustedDevice);
    }

    private static AuthSession CreateSession(
        DateTimeOffset? createdAtUtc = null)
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            new UserId(
                Guid.NewGuid()),
            "device-id-hash",
            "Test device",
            "127.0.0.1",
            "test-user-agent",
            createdAtUtc ??
            DateTimeOffset.UtcNow);
    }
}
