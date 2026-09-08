using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.RefreshTokens;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            7,
            10,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_ShouldCreateActiveToken()
    {
        var token =
            CreateToken();

        Assert.False(
            token.IsRevoked);

        Assert.True(
            token.IsActive(
                Now));

        Assert.Equal(
            "hashed-token",
            token.TokenHash);
    }

    [Fact]
    public void Create_WhenExpirationIsNotAfterCreation_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                RefreshToken.Create(
                    RefreshTokenId.New(),
                    RefreshTokenFamilyId.New(), UserId.New(),
                    "hashed-token",
                    Now,
                    Now));
    }

    [Fact]
    public void IsExpired_WhenCurrentTimeEqualsExpiration_ShouldReturnTrue()
    {
        var token =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(), UserId.New(),
                "hashed-token",
                Now,
                Now.AddHours(1));

        Assert.True(
            token.IsExpired(
                Now.AddHours(1)));
    }

    [Fact]
    public void Revoke_ShouldMakeTokenInactive()
    {
        var token =
            CreateToken();

        token.Revoke(
            Now.AddMinutes(5),
            "Logout");

        Assert.True(
            token.IsRevoked);

        Assert.False(
            token.IsActive(
                Now.AddMinutes(5)));

        Assert.Equal(
            Now.AddMinutes(5),
            token.RevokedAtUtc);

        Assert.Equal(
            "Logout",
            token.RevocationReason);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldBeIdempotent()
    {
        var token =
            CreateToken();

        token.Revoke(
            Now.AddMinutes(5),
            "First");

        token.Revoke(
            Now.AddMinutes(10),
            "Second");

        Assert.Equal(
            Now.AddMinutes(5),
            token.RevokedAtUtc);

        Assert.Equal(
            "First",
            token.RevocationReason);
    }

    [Fact]
    public void Rotate_ShouldRevokeTokenAndRecordReplacement()
    {
        var token =
            CreateToken();

        var replacementId =
            RefreshTokenId.New();

        token.Rotate(
            replacementId,
            Now.AddMinutes(5));

        Assert.True(
            token.IsRevoked);

        Assert.Equal(
            "Rotated",
            token.RevocationReason);

        Assert.Equal(
            replacementId,
            token.ReplacedByTokenId);

        Assert.False(
            token.IsActive(
                Now.AddMinutes(5)));
    }

    [Fact]
    public void Rotate_WhenAlreadyRevoked_ShouldNotChangeReplacement()
    {
        var token =
            CreateToken();

        var firstReplacement =
            RefreshTokenId.New();

        token.Rotate(
            firstReplacement,
            Now.AddMinutes(5));

        token.Rotate(
            RefreshTokenId.New(),
            Now.AddMinutes(10));

        Assert.Equal(
            firstReplacement,
            token.ReplacedByTokenId);

        Assert.Equal(
            Now.AddMinutes(5),
            token.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_WhenTimestampIsBeforeCreation_ShouldThrow()
    {
        var token =
            CreateToken();

        Assert.Throws<
            ArgumentException>(
            () =>
                token.Revoke(
                    Now.AddMinutes(-1),
                    "Invalid"));
    }

    [Fact]
    public void Rotate_WhenTimestampIsBeforeCreation_ShouldThrow()
    {
        var token =
            CreateToken();

        Assert.Throws<
            ArgumentException>(
            () =>
                token.Rotate(
                    RefreshTokenId.New(),
                    Now.AddMinutes(-1)));
    }

    private static RefreshToken CreateToken()
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(), UserId.New(),
            "hashed-token",
            Now,
            Now.AddDays(30));
    }
}

