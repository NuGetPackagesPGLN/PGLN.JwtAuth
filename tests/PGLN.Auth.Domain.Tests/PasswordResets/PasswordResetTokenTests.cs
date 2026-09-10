using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.PasswordResets;

public sealed class PasswordResetTokenTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            8,
            18,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Create_ShouldSetExpectedState()
    {
        var id =
            PasswordResetTokenId.New();

        var userId =
            UserId.New();

        var token =
            PasswordResetToken.Create(
                id,
                userId,
                "hashed-reset-token",
                Now,
                Now.AddMinutes(30));

        Assert.Equal(
            id,
            token.Id);

        Assert.Equal(
            userId,
            token.UserId);

        Assert.Equal(
            "hashed-reset-token",
            token.TokenHash);

        Assert.Equal(
            Now,
            token.CreatedAtUtc);

        Assert.Equal(
            Now.AddMinutes(30),
            token.ExpiresAtUtc);

        Assert.Null(
            token.UsedAtUtc);

        Assert.False(
            token.IsUsed);
    }

    [Fact]
    public void CanBeUsed_WithNewToken_ShouldReturnTrue()
    {
        var token =
            CreateToken();

        Assert.True(
            token.CanBeUsed(
                Now.AddMinutes(5)));
    }

    [Fact]
    public void IsExpired_AfterExpiration_ShouldReturnTrue()
    {
        var token =
            CreateToken();

        Assert.True(
            token.IsExpired(
                Now.AddMinutes(31)));
    }

    [Fact]
    public void IsExpired_AtExpirationBoundary_ShouldReturnTrue()
    {
        var token =
            CreateToken();

        Assert.True(
            token.IsExpired(
                Now.AddMinutes(30)));
    }

    [Fact]
    public void MarkAsUsed_ShouldRecordUsageTime()
    {
        var token =
            CreateToken();

        var usedAt =
            Now.AddMinutes(10);

        token.MarkAsUsed(
            usedAt);

        Assert.True(
            token.IsUsed);

        Assert.Equal(
            usedAt,
            token.UsedAtUtc);

        Assert.False(
            token.CanBeUsed(
                usedAt.AddMinutes(1)));
    }

    [Fact]
    public void MarkAsUsed_WhenAlreadyUsed_ShouldRemainIdempotent()
    {
        var token =
            CreateToken();

        var firstUsage =
            Now.AddMinutes(5);

        token.MarkAsUsed(
            firstUsage);

        token.MarkAsUsed(
            Now.AddMinutes(10));

        Assert.Equal(
            firstUsage,
            token.UsedAtUtc);
    }

    [Fact]
    public void MarkAsUsed_WhenExpired_ShouldThrow()
    {
        var token =
            CreateToken();

        Assert.Throws<
            InvalidOperationException>(
            () =>
                token.MarkAsUsed(
                    Now.AddMinutes(30)));
    }

    [Fact]
    public void MarkAsUsed_BeforeCreation_ShouldThrow()
    {
        var token =
            CreateToken();

        Assert.Throws<
            ArgumentException>(
            () =>
                token.MarkAsUsed(
                    Now.AddSeconds(-1)));
    }

    [Fact]
    public void Create_WithEmptyTokenHash_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    UserId.New(),
                    string.Empty,
                    Now,
                    Now.AddMinutes(30)));
    }

    [Fact]
    public void Create_WithInvalidExpiration_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    UserId.New(),
                    "hashed-reset-token",
                    Now,
                    Now));
    }

    [Fact]
    public void Create_WithEmptyId_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                PasswordResetToken.Create(
                    new PasswordResetTokenId(
                        Guid.Empty),
                    UserId.New(),
                    "hashed-reset-token",
                    Now,
                    Now.AddMinutes(30)));
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        Assert.Throws<
            ArgumentException>(
            () =>
                PasswordResetToken.Create(
                    PasswordResetTokenId.New(),
                    new UserId(
                        Guid.Empty),
                    "hashed-reset-token",
                    Now,
                    Now.AddMinutes(30)));
    }

    private static PasswordResetToken CreateToken()
    {
        return PasswordResetToken.Create(
            PasswordResetTokenId.New(),
            UserId.New(),
            "hashed-reset-token",
            Now,
            Now.AddMinutes(30));
    }
}
