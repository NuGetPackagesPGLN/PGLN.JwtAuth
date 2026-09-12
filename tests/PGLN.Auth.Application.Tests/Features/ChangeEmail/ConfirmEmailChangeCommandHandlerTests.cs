using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Features.ChangeEmail;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.Sessions;

namespace PGLN.Auth.Application.Tests.Features.ChangeEmail;

public sealed class ConfirmEmailChangeCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            12,
            10,
            0,
            0,
            TimeSpan.Zero);

    private const string CurrentEmail =
        "current@example.com";

    private const string NewEmail =
        "new@example.com";

    private const string RawToken =
        "email-change-token";

    private const string TokenHash =
        "hashed::email-change-token";

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldChangeEmail()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        var token =
            CreateToken(
                user.Id);

        tokens.Seed(
            token);

        var result =
            await CreateHandler(
                    users,
                    tokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            NewEmail,
            user.Email.Value);

        Assert.Equal(
            Email.Create(NewEmail).NormalizedValue,
            user.NormalizedEmail);

        Assert.True(
            user.EmailConfirmed);

        Assert.Equal(
            Now,
            user.EmailConfirmedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldMarkTokenAsUsed()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        var token =
            CreateToken(
                user.Id);

        tokens.Seed(
            token);

        var result =
            await CreateHandler(
                    users,
                    tokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.True(
            token.IsUsed);

        Assert.Equal(
            Now,
            token.UsedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ShouldFail()
    {
        var users =
            new FakeUserRepository();

        users.Seed(
            CreateUser());

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    new FakeEmailChangeTokenRepository(),
                    unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ConfirmEmailChangeErrors.InvalidToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ShouldFail()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        var expiredToken =
            EmailChangeToken.Create(
                EmailChangeTokenId.New(),
                user.Id,
                Email.Create(NewEmail),
                TokenHash,
                Now.AddHours(-2),
                Now.AddSeconds(-1));

        tokens.Seed(
            expiredToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ConfirmEmailChangeErrors.ExpiredToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            CurrentEmail,
            user.Email.Value);

        Assert.False(
            expiredToken.IsUsed);
    }

    [Fact]
    public async Task HandleAsync_WithUsedToken_ShouldFail()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        var token =
            CreateToken(
                user.Id);

        token.MarkAsUsed(
            Now.AddMinutes(-1));

        tokens.Seed(
            token);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ConfirmEmailChangeErrors.TokenAlreadyUsed,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            CurrentEmail,
            user.Email.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldFail()
    {
        var missingUserId =
            UserId.New();

        var tokens =
            new FakeEmailChangeTokenRepository();

        var token =
            CreateToken(
                missingUserId);

        tokens.Seed(
            token);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    new FakeUserRepository(),
                    tokens,
                    unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ConfirmEmailChangeErrors.UserNotFound,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.False(
            token.IsUsed);
    }

    [Fact]
    public async Task HandleAsync_WhenNewEmailBelongsToAnotherUser_ShouldFail()
    {
        var user =
            CreateUser();

        var otherUser =
            CreateUser(
                NewEmail);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        users.Seed(
            otherUser);

        var tokens =
            new FakeEmailChangeTokenRepository();

        var token =
            CreateToken(
                user.Id);

        tokens.Seed(
            token);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ConfirmEmailChangeErrors.EmailAlreadyInUse,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            CurrentEmail,
            user.Email.Value);

        Assert.False(
            token.IsUsed);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldRevokeOtherActiveTokens()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        var confirmedToken =
            CreateToken(
                user.Id);

        var otherToken =
            EmailChangeToken.Create(
                EmailChangeTokenId.New(),
                user.Id,
                Email.Create("another@example.com"),
                "hashed::another-token",
                Now.AddMinutes(-10),
                Now.AddMinutes(20));

        tokens.Seed(
            confirmedToken);

        tokens.Seed(
            otherToken);

        var result =
            await CreateHandler(
                    users,
                    tokens)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.True(
            confirmedToken.IsUsed);

        Assert.True(
            otherToken.IsUsed);

        Assert.Equal(
            Now,
            otherToken.UsedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldSaveChangesOnce()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        tokens.Seed(
            CreateToken(
                user.Id));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    unitOfWork)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }


    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldPublishEmailChangedNotification()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        tokens.Seed(
            CreateToken(
                user.Id));

        var integrationEventPublisher =
            new FakeIntegrationEventPublisher();

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    integrationEventPublisher:
                        integrationEventPublisher)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        var integrationEvent =
            Assert.Single(
                integrationEventPublisher.Events);

        var emailChangedEvent =
            Assert.IsType<EmailChangedNotificationRequested>(
                integrationEvent);

        Assert.Equal(
            user.Id,
            emailChangedEvent.UserId);

        Assert.Equal(
            CurrentEmail,
            emailChangedEvent.OldEmail);

        Assert.Equal(
            NewEmail,
            emailChangedEvent.NewEmail);

        Assert.Equal(
            Now,
            emailChangedEvent.OccurredAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldRevokeAllActiveSessions()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        tokens.Seed(
            CreateToken(
                user.Id));

        var sessions =
            new FakeAuthSessionRepository();

        var firstSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-001",
                "Chrome",
                "192.168.1.10",
                "Mozilla/5.0",
                Now.AddDays(-2));

        var secondSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-002",
                "Edge",
                "192.168.1.20",
                "Mozilla/5.0",
                Now.AddDays(-1));

        sessions.Seed(
            firstSession);

        sessions.Seed(
            secondSession);

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    authSessionRepository:
                        sessions)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.All(
            sessions.Sessions,
            session =>
            {
                Assert.True(
                    session.IsRevoked);

                Assert.Equal(
                    Now,
                    session.RevokedAtUtc);

                Assert.Equal(
                    "Email address changed.",
                    session.RevocationReason);
            });
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveAlreadyRevokedSessionsUnchanged()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeEmailChangeTokenRepository();

        tokens.Seed(
            CreateToken(
                user.Id));

        var sessions =
            new FakeAuthSessionRepository();

        var revokedAt =
            Now.AddHours(-3);

        var session =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-001",
                "Chrome",
                "192.168.1.10",
                "Mozilla/5.0",
                Now.AddDays(-1));

        session.Revoke(
            revokedAt,
            "User logged out.");

        sessions.Seed(
            session);

        var result =
            await CreateHandler(
                    users,
                    tokens,
                    authSessionRepository:
                        sessions)
                .HandleAsync(
                    CreateCommand());

        Assert.True(
            result.IsSuccess);

        Assert.True(
            session.IsRevoked);

        Assert.Equal(
            revokedAt,
            session.RevokedAtUtc);

        Assert.Equal(
            "User logged out.",
            session.RevocationReason);
    }
    private static ConfirmEmailChangeCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeEmailChangeTokenRepository emailChangeTokenRepository,
        FakeUnitOfWork? unitOfWork = null,
        FakeIntegrationEventPublisher? integrationEventPublisher = null,
        FakeAuthSessionRepository? authSessionRepository = null)
    {
        return new ConfirmEmailChangeCommandHandler(
            userRepository,
            emailChangeTokenRepository,
            authSessionRepository ??
                new FakeAuthSessionRepository(),
            new FakeTokenHasher(),
            integrationEventPublisher ??
                new FakeIntegrationEventPublisher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now));
    }

    private static ConfirmEmailChangeCommand CreateCommand()
    {
        return new ConfirmEmailChangeCommand(
            RawToken);
    }

    private static User CreateUser(
        string email =
            CurrentEmail)
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    email),
                "password-hash",
                Now.AddDays(-30));

        user.ClearDomainEvents();

        return user;
    }

    private static EmailChangeToken CreateToken(
        UserId userId)
    {
        return EmailChangeToken.Create(
            EmailChangeTokenId.New(),
            userId,
            Email.Create(
                NewEmail),
            TokenHash,
            Now.AddMinutes(-5),
            Now.AddMinutes(25));
    }
}







