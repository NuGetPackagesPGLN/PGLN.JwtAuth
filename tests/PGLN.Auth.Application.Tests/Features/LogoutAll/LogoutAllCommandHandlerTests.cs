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
    public async Task HandleAsync_ShouldRevokeAllActiveUserSessions()
    {
        var userId =
            UserId.New();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var anchor =
            CreateActiveToken(
                userId,
                "hashed::anchor-token");

        refreshTokenRepository.Seed(
            anchor);

        var sessionRepository =
            new FakeAuthSessionRepository();

        var firstSession =
            CreateSession(
                userId,
                "device-one");

        var secondSession =
            CreateSession(
                userId,
                "device-two");

        sessionRepository.Seed(
            firstSession);

        sessionRepository.Seed(
            secondSession);

        var result =
            await CreateHandler(
                    refreshTokenRepository,
                    authSessionRepository:
                        sessionRepository)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsSuccess);

        Assert.All(
            new[]
            {
                firstSession,
                secondSession
            },
            session =>
            {
                Assert.True(
                    session.IsRevoked);

                Assert.Equal(
                    Now,
                    session.RevokedAtUtc);

                Assert.Equal(
                    "LogoutAll",
                    session.RevocationReason);
            });
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRevokeAnotherUsersSessions()
    {
        var targetUserId =
            UserId.New();

        var unrelatedUserId =
            UserId.New();

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        refreshTokenRepository.Seed(
            CreateActiveToken(
                targetUserId,
                "hashed::anchor-token"));

        var sessionRepository =
            new FakeAuthSessionRepository();

        var targetSession =
            CreateSession(
                targetUserId,
                "target-device");

        var unrelatedSession =
            CreateSession(
                unrelatedUserId,
                "unrelated-device");

        sessionRepository.Seed(
            targetSession);

        sessionRepository.Seed(
            unrelatedSession);

        var result =
            await CreateHandler(
                    refreshTokenRepository,
                    authSessionRepository:
                        sessionRepository)
                .HandleAsync(
                    new LogoutAllCommand(
                        "anchor-token"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            targetSession.IsRevoked);

        Assert.False(
            unrelatedSession.IsRevoked);
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

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            CreateSession(
                userId,
                "device-one"));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork,
                    sessionRepository)
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
        FakeUnitOfWork? unitOfWork = null,
        FakeAuthSessionRepository? authSessionRepository = null)
    {
        return new LogoutAllCommandHandler(
            repository,
            authSessionRepository ??
                new FakeAuthSessionRepository(),
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

    private static AuthSession CreateSession(
        UserId userId,
        string deviceIdHash)
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            userId,
            deviceIdHash,
            "Test Device",
            "127.0.0.1",
            "Test User Agent",
            Now.AddDays(-1));
    }
}
