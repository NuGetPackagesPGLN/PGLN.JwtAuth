using PGLN.Auth.Application.Features.Logout;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.Logout;

public sealed class LogoutCommandHandlerTests
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
    public async Task HandleAsync_WithUnknownToken_ShouldSucceed()
    {
        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                new FakeRefreshTokenRepository(),
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new LogoutCommand(
                    "unknown-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithActiveToken_ShouldRevokeToken()
    {
        var token =
            CreateActiveToken();

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            token);

        var result =
            await CreateHandler(
                    repository)
                .HandleAsync(
                    new LogoutCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            token.IsRevoked);

        Assert.Equal(
            "Logout",
            token.RevocationReason);

        Assert.Equal(
            Now,
            token.RevokedAtUtc);

        Assert.Null(
            token.ReplacedByTokenId);
    }

    [Fact]
    public async Task HandleAsync_WithActiveToken_ShouldRevokeLinkedSession()
    {
        var userId =
            UserId.New();

        var sessionId =
            AuthSessionId.New();

        var session =
            AuthSession.Create(
                sessionId,
                userId,
                "device-hash",
                "Test Device",
                "127.0.0.1",
                "Test User Agent",
                Now.AddDays(-1));

        var refreshToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                userId,
                sessionId,
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        refreshTokenRepository.Seed(
            refreshToken);

        var authSessionRepository =
            new FakeAuthSessionRepository();

        authSessionRepository.Seed(
            session);

        var result =
            await CreateHandler(
                    refreshTokenRepository,
                    authSessionRepository:
                        authSessionRepository)
                .HandleAsync(
                    new LogoutCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            session.IsRevoked);

        Assert.Equal(
            Now,
            session.RevokedAtUtc);

        Assert.Equal(
            "Logout",
            session.RevocationReason);
    }

    [Fact]
    public async Task HandleAsync_WithActiveToken_ShouldSaveChangesOnce()
    {
        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            CreateActiveToken());

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork)
                .HandleAsync(
                    new LogoutCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithAlreadyRevokedToken_ShouldSucceedWithoutSaving()
    {
        var token =
            CreateActiveToken();

        token.Revoke(
            Now.AddMinutes(-1),
            "Logout");

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            token);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork)
                .HandleAsync(
                    new LogoutCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ShouldSucceedWithoutSaving()
    {
        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                UserId.New(),
                AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-31),
                Now.AddSeconds(-1)));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    unitOfWork)
                .HandleAsync(
                    new LogoutCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    private static LogoutCommandHandler CreateHandler(
        FakeRefreshTokenRepository repository,
        FakeUnitOfWork? unitOfWork = null,
        FakeAuthSessionRepository? authSessionRepository = null)
    {
        return new LogoutCommandHandler(
            repository,
            authSessionRepository ??
                new FakeAuthSessionRepository(),
            new FakeTokenHasher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now));
    }

    private static RefreshToken CreateActiveToken()
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            UserId.New(),
            AuthSessionId.New(),
            "hashed::raw-refresh-token",
            Now.AddDays(-1),
            Now.AddDays(29));
    }
}
