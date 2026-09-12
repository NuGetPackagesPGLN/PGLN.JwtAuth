using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.Sessions;

public sealed class AuthSessionTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(
            2026,
            9,
            11,
            5,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_ShouldCreateActiveSession()
    {
        var session =
            AuthSession.Create(
                AuthSessionId.New(),
                UserId.New(),
                "device-hash",
                "Chrome on Windows",
                "127.0.0.1",
                "Mozilla/5.0",
                CreatedAtUtc);

        Assert.False(
            session.IsRevoked);

        Assert.True(
            session.IsActive);

        Assert.Equal(
            CreatedAtUtc,
            session.CreatedAtUtc);

        Assert.Equal(
            CreatedAtUtc,
            session.LastSeenAtUtc);

        Assert.Equal(
            "device-hash",
            session.DeviceIdHash);
    }

    [Fact]
    public void Create_WithEmptyDeviceIdHash_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () =>
                AuthSession.Create(
                    AuthSessionId.New(),
                    UserId.New(),
                    string.Empty,
                    null,
                    null,
                    null,
                    CreatedAtUtc));
    }

    [Fact]
    public void Touch_ShouldUpdateLastSeenIpAddressAndUserAgent()
    {
        var session =
            CreateSession();

        var seenAtUtc =
            CreatedAtUtc.AddMinutes(
                10);

        session.Touch(
            seenAtUtc,
            "10.0.0.5",
            "New User Agent");

        Assert.Equal(
            seenAtUtc,
            session.LastSeenAtUtc);

        Assert.Equal(
            "10.0.0.5",
            session.IpAddress);

        Assert.Equal(
            "New User Agent",
            session.UserAgent);
    }

    [Fact]
    public void Touch_WithTimeBeforeCreation_ShouldThrow()
    {
        var session =
            CreateSession();

        Assert.Throws<ArgumentException>(
            () =>
                session.Touch(
                    CreatedAtUtc.AddMinutes(
                        -1)));
    }

    [Fact]
    public void Revoke_ShouldMarkSessionAsRevoked()
    {
        var session =
            CreateSession();

        var revokedAtUtc =
            CreatedAtUtc.AddMinutes(
                30);

        session.Revoke(
            revokedAtUtc,
            "UserRequested");

        Assert.True(
            session.IsRevoked);

        Assert.False(
            session.IsActive);

        Assert.Equal(
            revokedAtUtc,
            session.RevokedAtUtc);

        Assert.Equal(
            "UserRequested",
            session.RevocationReason);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldNotOverwriteExistingRevocation()
    {
        var session =
            CreateSession();

        var firstRevocation =
            CreatedAtUtc.AddMinutes(
                10);

        session.Revoke(
            firstRevocation,
            "FirstReason");

        session.Revoke(
            CreatedAtUtc.AddMinutes(
                20),
            "SecondReason");

        Assert.Equal(
            firstRevocation,
            session.RevokedAtUtc);

        Assert.Equal(
            "FirstReason",
            session.RevocationReason);
    }

    [Fact]
    public void Touch_WhenSessionIsRevoked_ShouldNotUpdateSession()
    {
        var session =
            CreateSession();

        session.Revoke(
            CreatedAtUtc.AddMinutes(
                5),
            "UserRequested");

        session.Touch(
            CreatedAtUtc.AddMinutes(
                10),
            "10.0.0.10",
            "Different User Agent");

        Assert.Equal(
            CreatedAtUtc,
            session.LastSeenAtUtc);

        Assert.Equal(
            "127.0.0.1",
            session.IpAddress);

        Assert.Equal(
            "Mozilla/5.0",
            session.UserAgent);
    }

    private static AuthSession CreateSession()
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            UserId.New(),
            "device-hash",
            "Chrome on Windows",
            "127.0.0.1",
            "Mozilla/5.0",
            CreatedAtUtc);
    }
}
