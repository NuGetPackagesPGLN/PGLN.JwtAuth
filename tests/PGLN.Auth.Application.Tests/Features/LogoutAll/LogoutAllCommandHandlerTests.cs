using PGLN.Auth.Application.Features.LogoutAll;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.LogoutAll;

public sealed class LogoutAllCommandHandlerTests
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
    public async Task HandleAsync_WithActiveToken_ShouldRevokeAllActiveUserTokens()
    {
        var userId =
            UserId.New();

        var repository =
            new FakeRefreshTokenRepository();

        var first =
            CreateActiveToken(
                userId,
                "hashed::anchor-token");

        var second =
            CreateActiveToken(
                userId,
                "hashed::second-token");

        var third =
            CreateActiveToken(
                userId,
                "hashed::third-token");

        repository.Seed(
            first);

        repository.Seed(
            second);

        repository.Seed(
            third);

        var result =
            await CreateHandler(
                    repository)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsSuccess);

        Assert.All(
            new[]
            {
                first,
                second,
                third
            },
            token =>
            {
                Assert.True(
                    token.IsRevoked);

                Assert.Equal(
                    "LogoutAll",
                    token.RevocationReason);

                Assert.Equal(
                    Now,
                    token.RevokedAtUtc);
            });
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRevokeAnotherUsersTokens()
    {
        var targetUserId =
            UserId.New();

        var unrelatedUserId =
            UserId.New();

        var repository =
            new FakeRefreshTokenRepository();

        var anchor =
            CreateActiveToken(
                targetUserId,
                "hashed::anchor-token");

        var targetSecond =
            CreateActiveToken(
                targetUserId,
                "hashed::target-second");

        var unrelated =
            CreateActiveToken(
                unrelatedUserId,
                "hashed::unrelated");

        repository.Seed(
            anchor);

        repository.Seed(
            targetSecond);

        repository.Seed(
            unrelated);

        var result =
            await CreateHandler(
                    repository)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            anchor.IsRevoked);

        Assert.True(
            targetSecond.IsRevoked);

        Assert.False(
            unrelated.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ShouldFail()
    {
        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    new FakeRefreshTokenRepository(),
                    unitOfWork)
                .HandleAsync(
                    new LogoutAllCommand(
                        "unknown-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LogoutAllErrors.InvalidToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithRevokedAnchorToken_ShouldFail()
    {
        var repository =
            new FakeRefreshTokenRepository();

        var token =
            CreateActiveToken(
                UserId.New(),
                "hashed::anchor-token");

        token.Revoke(
            Now.AddMinutes(-1),
            "Logout");

        repository.Seed(
            token);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LogoutAllErrors.RevokedToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredAnchorToken_ShouldFail()
    {
        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                UserId.New(),
                AuthSessionId.New(),
                "hashed::anchor-token",
                Now.AddDays(-31),
                Now.AddSeconds(-1)));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LogoutAllErrors.ExpiredToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveChangesOnce()
    {
        var userId =
            UserId.New();

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            CreateActiveToken(
                userId,
                "hashed::anchor-token"));

        repository.Seed(
            CreateActiveToken(
                userId,
                "hashed::second-token"));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static LogoutAllCommandHandler CreateHandler(
        FakeRefreshTokenRepository repository,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new LogoutAllCommandHandler(
            repository,
            new FakeTokenHasher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now));
    }

    private static RefreshToken CreateActiveToken(
        UserId userId,
        string tokenHash)
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            userId,
            AuthSessionId.New(),
            tokenHash,
            Now.AddDays(-1),
            Now.AddDays(29));
    }
}


