using PGLN.Auth.Application.Features.Sessions.RevokeSession;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.Sessions.RevokeSession;

public sealed class RevokeSessionCommandHandlerTests
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
    public async Task HandleAsync_WithOwnedSession_ShouldRevokeSession()
    {
        var userId =
            UserId.New();

        var session =
            CreateSession(
                userId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var clock =
            new FakeClock(
                Now);

        var handler =
            new RevokeSessionCommandHandler(
                sessionRepository,
                refreshTokenRepository,
                unitOfWork,
                clock);

        var result =
            await handler.HandleAsync(
                new RevokeSessionCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            session.IsRevoked);

        Assert.Equal(
            Now,
            session.RevokedAtUtc);

        Assert.Equal(
            "UserRevokedSession",
            session.RevocationReason);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithOwnedSession_ShouldRevokeAllRefreshTokensForSession()
    {
        var userId =
            UserId.New();

        var session =
            CreateSession(
                userId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var firstToken =
            CreateRefreshToken(
                userId,
                session.Id,
                "token-one");

        var secondToken =
            CreateRefreshToken(
                userId,
                session.Id,
                "token-two");

        refreshTokenRepository.Seed(
            firstToken);

        refreshTokenRepository.Seed(
            secondToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RevokeSessionCommandHandler(
                sessionRepository,
                refreshTokenRepository,
                unitOfWork,
                new FakeClock(Now));

        var result =
            await handler.HandleAsync(
                new RevokeSessionCommand(
                    userId,
                    session.Id));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            firstToken.IsRevoked);

        Assert.True(
            secondToken.IsRevoked);

        Assert.Equal(
            Now,
            firstToken.RevokedAtUtc);

        Assert.Equal(
            Now,
            secondToken.RevokedAtUtc);

        Assert.Equal(
            "SessionRevoked",
            firstToken.RevocationReason);

        Assert.Equal(
            "SessionRevoked",
            secondToken.RevocationReason);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRevokeRefreshTokensFromAnotherSession()
    {
        var userId =
            UserId.New();

        var firstSession =
            CreateSession(
                userId,
                "device-one");

        var secondSession =
            CreateSession(
                userId,
                "device-two");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            firstSession);

        sessionRepository.Seed(
            secondSession);

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var firstSessionToken =
            CreateRefreshToken(
                userId,
                firstSession.Id,
                "token-one");

        var secondSessionToken =
            CreateRefreshToken(
                userId,
                secondSession.Id,
                "token-two");

        refreshTokenRepository.Seed(
            firstSessionToken);

        refreshTokenRepository.Seed(
            secondSessionToken);

        var handler =
            new RevokeSessionCommandHandler(
                sessionRepository,
                refreshTokenRepository,
                new FakeUnitOfWork(),
                new FakeClock(Now));

        var result =
            await handler.HandleAsync(
                new RevokeSessionCommand(
                    userId,
                    firstSession.Id));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            firstSessionToken.IsRevoked);

        Assert.False(
            secondSessionToken.IsRevoked);

        Assert.False(
            secondSession.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_WithAnotherUsersSession_ShouldReturnFailure()
    {
        var sessionOwnerId =
            UserId.New();

        var requestingUserId =
            UserId.New();

        var session =
            CreateSession(
                sessionOwnerId,
                "device-one");

        var sessionRepository =
            new FakeAuthSessionRepository();

        sessionRepository.Seed(
            session);

        var refreshTokenRepository =
            new FakeRefreshTokenRepository();

        var refreshToken =
            CreateRefreshToken(
                sessionOwnerId,
                session.Id,
                "token-one");

        refreshTokenRepository.Seed(
            refreshToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RevokeSessionCommandHandler(
                sessionRepository,
                refreshTokenRepository,
                unitOfWork,
                new FakeClock(Now));

        var result =
            await handler.HandleAsync(
                new RevokeSessionCommand(
                    requestingUserId,
                    session.Id));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.NotFound",
            result.Error.Code);

        Assert.False(
            session.IsRevoked);

        Assert.False(
            refreshToken.IsRevoked);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownSession_ShouldReturnFailure()
    {
        var userId =
            UserId.New();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RevokeSessionCommandHandler(
                new FakeAuthSessionRepository(),
                new FakeRefreshTokenRepository(),
                unitOfWork,
                new FakeClock(Now));

        var result =
            await handler.HandleAsync(
                new RevokeSessionCommand(
                    userId,
                    AuthSessionId.New()));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            "Sessions.NotFound",
            result.Error.Code);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
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
            "PGLN.Auth.Application.Tests",
            Now.AddDays(-1));
    }

    private static RefreshToken CreateRefreshToken(
        UserId userId,
        AuthSessionId sessionId,
        string tokenHash)
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            userId,
            sessionId,
            tokenHash,
            Now.AddHours(-1),
            Now.AddDays(7));
    }
}
