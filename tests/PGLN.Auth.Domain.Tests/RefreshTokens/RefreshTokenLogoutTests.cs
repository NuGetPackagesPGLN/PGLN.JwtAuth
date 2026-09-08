using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.RefreshTokens;

public sealed class RefreshTokenLogoutTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            8,
            10,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Revoke_ShouldMarkTokenAsRevoked()
    {
        var token =
            CreateToken();

        token.Revoke(
            Now.AddHours(1),
            "Logout");

        Assert.True(
            token.IsRevoked);

        Assert.Equal(
            Now.AddHours(1),
            token.RevokedAtUtc);

        Assert.Equal(
            "Logout",
            token.RevocationReason);

        Assert.Null(
            token.ReplacedByTokenId);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldBeIdempotent()
    {
        var token =
            CreateToken();

        token.Revoke(
            Now.AddHours(1),
            "Logout");

        token.Revoke(
            Now.AddHours(2),
            "Another reason");

        Assert.Equal(
            Now.AddHours(1),
            token.RevokedAtUtc);

        Assert.Equal(
            "Logout",
            token.RevocationReason);
    }

    private static RefreshToken CreateToken()
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(), UserId.New(),
            "HASHED-TOKEN",
            Now,
            Now.AddDays(30));
    }
}

