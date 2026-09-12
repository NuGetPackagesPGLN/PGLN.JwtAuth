using PGLN.Auth.Application.Features.Sessions.RevokeOtherSessions;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.Sessions.RevokeOtherSessions;

public sealed class RevokeOtherSessionsCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            11,
            10,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldRevokeAllOtherSessions()
    {
        var userId =
            UserId.New();

        var currentSession =
            CreateSession(
                userId,
                "current-device");

        var otherSession =
            CreateSession(
                userId,
                "other-device");

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            currentSession);

        sessions.Seed(
            otherSession);

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var handler =
            CreateHandler(
                sessions,
                refreshTokens);

        var result =
            await handler.HandleAsync(
                new RevokeOtherSessionsCommand(
                    userId,
                    currentSession.Id));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            currentSession.IsRevoked);

        Assert.True(
            otherSession.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeRefreshTokensForOtherSessions()
    {
        var userId =
            UserId.New();

        var currentSession =
            CreateSession(
                userId,
                "current-device");

        var otherSession =
            CreateSession(
                userId,
                "other-device");

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            currentSession);

        sessions.Seed(
            otherSession);

        var currentToken =
            CreateRefreshToken(
                userId,
                currentSession.Id);

        var otherToken =
            CreateRefreshToken(
                userId,
                otherSession.Id);

        var refreshTokens =
            new FakeRefreshTokenRepository();

        refreshTokens.Seed(
            currentToken);

        refreshTokens.Seed(
            otherToken);

        var handler =
            CreateHandler(
                sessions,
                refreshTokens);

        var result =
            await handler.HandleAsync(
                new RevokeOtherSessionsCommand(
                    userId,
                    currentSession.Id));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            currentToken.IsRevoked);

        Assert.True(
            otherToken.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepCurrentSessionActive()
    {
        var userId =
            UserId.New();

        var currentSession =
            CreateSession(
                userId,
                "current-device");

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            currentSession);

        var handler =
            CreateHandler(
                sessions,
                new FakeRefreshTokenRepository());

        var result =
            await handler.HandleAsync(
                new RevokeOtherSessionsCommand(
                    userId,
                    currentSession.Id));

        Assert.True(
            result.IsSuccess);

        Assert.False(
            currentSession.IsRevoked);

        Assert.True(
            currentSession.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentSessionBelongsToAnotherUser_ShouldFail()
    {
        var userId =
            UserId.New();

        var anotherUserId =
            UserId.New();

        var session =
            CreateSession(
                anotherUserId,
                "device");

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            session);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                sessions,
                new FakeRefreshTokenRepository(),
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new RevokeOtherSessionsCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.CurrentSessionNotFound",
            result.Error.Code);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentSessionDoesNotExist_ShouldFail()
    {
        var handler =
            CreateHandler(
                new FakeAuthSessionRepository(),
                new FakeRefreshTokenRepository());

        var result =
            await handler.HandleAsync(
                new RevokeOtherSessionsCommand(
                    UserId.New(),
                    AuthSessionId.New()));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.CurrentSessionNotFound",
            result.Error.Code);
    }

    private static RevokeOtherSessionsCommandHandler CreateHandler(
        FakeAuthSessionRepository sessions,
        FakeRefreshTokenRepository refreshTokens,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new RevokeOtherSessionsCommandHandler(
            sessions,
            refreshTokens,
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now));
    }

    private static AuthSession CreateSession(
        UserId userId,
        string deviceIdHash)
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            userId,
            deviceIdHash,
            deviceIdHash,
            "127.0.0.1",
            "TestAgent/1.0",
            Now.AddDays(-1));
    }

    private static RefreshToken CreateRefreshToken(
        UserId userId,
        AuthSessionId sessionId)
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            userId,
            sessionId,
            $"hash-{Guid.NewGuid()}",
            Now.AddDays(-1),
            Now.AddDays(29));
    }
}
