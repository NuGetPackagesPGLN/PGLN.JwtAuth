using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Domain.Tests.VerificationTokens;

public sealed class EmailVerificationTokenTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(
            2026,
            9,
            6,
            10,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset ExpiresAt =
        CreatedAt.AddHours(24);

    [Fact]
    public void Create_WithValidValues_ShouldCreateToken()
    {
        var id =
            EmailVerificationTokenId.New();

        var userId =
            UserId.New();

        var token =
            EmailVerificationToken.Create(
                id,
                userId,
                "hashed-verification-token",
                CreatedAt,
                ExpiresAt);

        Assert.Equal(
            id,
            token.Id);

        Assert.Equal(
            userId,
            token.UserId);

        Assert.Equal(
            "hashed-verification-token",
            token.TokenHash);

        Assert.Equal(
            CreatedAt,
            token.CreatedAtUtc);

        Assert.Equal(
            ExpiresAt,
            token.ExpiresAtUtc);

        Assert.False(
            token.IsUsed);

        Assert.Null(
            token.UsedAtUtc);
    }

    [Fact]
    public void Create_WhenExpirationIsEqualToCreation_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () =>
                EmailVerificationToken.Create(
                    EmailVerificationTokenId.New(),
                    UserId.New(),
                    "hash",
                    CreatedAt,
                    CreatedAt));
    }

    [Fact]
    public void Create_WhenExpirationIsBeforeCreation_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () =>
                EmailVerificationToken.Create(
                    EmailVerificationTokenId.New(),
                    UserId.New(),
                    "hash",
                    CreatedAt,
                    CreatedAt.AddMinutes(-1)));
    }

    [Fact]
    public void IsExpired_BeforeExpiration_ShouldReturnFalse()
    {
        var token =
            CreateToken();

        var result =
            token.IsExpired(
                ExpiresAt.AddSeconds(-1));

        Assert.False(result);
    }

    [Fact]
    public void IsExpired_AtExpiration_ShouldReturnTrue()
    {
        var token =
            CreateToken();

        var result =
            token.IsExpired(
                ExpiresAt);

        Assert.True(result);
    }

    [Fact]
    public void CanBeUsed_WhenUnusedAndNotExpired_ShouldReturnTrue()
    {
        var token =
            CreateToken();

        var result =
            token.CanBeUsed(
                CreatedAt.AddHours(1));

        Assert.True(result);
    }

    [Fact]
    public void CanBeUsed_WhenExpired_ShouldReturnFalse()
    {
        var token =
            CreateToken();

        var result =
            token.CanBeUsed(
                ExpiresAt);

        Assert.False(result);
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedAt()
    {
        var token =
            CreateToken();

        var usedAt =
            CreatedAt.AddHours(2);

        token.MarkAsUsed(
            usedAt);

        Assert.True(
            token.IsUsed);

        Assert.Equal(
            usedAt,
            token.UsedAtUtc);
    }

    [Fact]
    public void CanBeUsed_AfterMarkAsUsed_ShouldReturnFalse()
    {
        var token =
            CreateToken();

        token.MarkAsUsed(
            CreatedAt.AddHours(2));

        var result =
            token.CanBeUsed(
                CreatedAt.AddHours(3));

        Assert.False(result);
    }

    [Fact]
    public void MarkAsUsed_WhenAlreadyUsed_ShouldBeIdempotent()
    {
        var token =
            CreateToken();

        var firstUsedAt =
            CreatedAt.AddHours(1);

        token.MarkAsUsed(
            firstUsedAt);

        token.MarkAsUsed(
            CreatedAt.AddHours(2));

        Assert.Equal(
            firstUsedAt,
            token.UsedAtUtc);
    }

    [Fact]
    public void MarkAsUsed_WhenUsageTimeIsBeforeCreation_ShouldThrow()
    {
        var token =
            CreateToken();

        Assert.Throws<ArgumentException>(
            () =>
                token.MarkAsUsed(
                    CreatedAt.AddSeconds(-1)));
    }

    private static EmailVerificationToken CreateToken()
    {
        return EmailVerificationToken.Create(
            EmailVerificationTokenId.New(),
            UserId.New(),
            "hashed-verification-token",
            CreatedAt,
            ExpiresAt);
    }
}
