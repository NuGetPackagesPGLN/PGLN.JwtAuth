using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.TokenRefresh;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.TokenRefresh;

public sealed class TokenRefreshCommandHandlerTests
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

    private static readonly RefreshTokenOptions Options =
        new()
        {
            TokenLifetime =
                TimeSpan.FromDays(30)
        };

    [Fact]
    public async Task HandleAsync_WithUnknownToken_ShouldReturnInvalidToken()
    {
        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var handler =
            CreateHandler(
                new FakeRefreshTokenRepository(),
                new FakeUserRepository(),
                refreshTokenGenerator,
                accessTokenGenerator,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new TokenRefreshCommand(
                    "unknown-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.InvalidToken,
            result.Error);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ShouldReturnExpiredToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(), user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-31),
                Now.AddSeconds(-1)));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.ExpiredToken,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);

        Assert.Single(
            tokens.Tokens);
    }

    [Fact]
    public async Task HandleAsync_WithReusedRotatedToken_ShouldReturnRevokedToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var token =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(), user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        token.Rotate(
            RefreshTokenId.New(),
            Now.AddMinutes(-1));

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            token);

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    refreshTokenGenerator,
                    accessTokenGenerator,
                    unitOfWork)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnUserNotFound()
    {
        var missingUserId =
            UserId.New();

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(), missingUserId, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29)));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    tokens,
                    new FakeUserRepository(),
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.UserNotFound,
            result.Error);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldRotateExistingToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var existing =
            CreateActiveToken(
                user.Id);

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var result =
            await CreateHandler(
                    tokens,
                    users)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.True(
            existing.IsRevoked);

        Assert.Equal(
            "Rotated",
            existing.RevocationReason);

        Assert.NotNull(
            existing.ReplacedByTokenId);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldPersistReplacementToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var existing =
            CreateActiveToken(
                user.Id);

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var generator =
            new FakeRefreshTokenGenerator
            {
                Token =
                    "new-raw-refresh-token"
            };

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    refreshTokenGenerator:
                        generator)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            2,
            tokens.Tokens.Count);

        var replacement =
            tokens.Tokens.Single(
                token =>
                    token.Id != existing.Id);

        Assert.Equal(
            user.Id,
            replacement.UserId);

        Assert.Equal(
            "hashed::new-raw-refresh-token",
            replacement.TokenHash);

        Assert.NotEqual(
            "new-raw-refresh-token",
            replacement.TokenHash);

        Assert.Equal(
            Now,
            replacement.CreatedAtUtc);

        Assert.Equal(
            Now.AddDays(30),
            replacement.ExpiresAtUtc);

        Assert.Equal(
            replacement.Id,
            existing.ReplacedByTokenId);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldReturnNewRawRefreshToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            CreateActiveToken(
                user.Id));

        var generator =
            new FakeRefreshTokenGenerator
            {
                Token =
                    "new-raw-refresh-token"
            };

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    refreshTokenGenerator:
                        generator)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "new-raw-refresh-token",
            result.Value.RefreshToken);

        Assert.Equal(
            Now.AddDays(30),
            result.Value.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldIssueNewAccessToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            CreateActiveToken(
                user.Id));

        var accessTokenGenerator =
            new FakeAccessTokenGenerator
            {
                Token =
                    "new-access-token"
            };

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    accessTokenGenerator:
                        accessTokenGenerator)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            "new-access-token",
            result.Value.AccessToken);

        Assert.Equal(
            1,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            user.Id.Value,
            result.Value.UserId);

        Assert.Equal(
            user.Email.Value,
            result.Value.Email);
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
            new FakeRefreshTokenRepository();

        tokens.Seed(
            CreateActiveToken(
                user.Id));

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static TokenRefreshCommandHandler CreateHandler(
        FakeRefreshTokenRepository refreshTokenRepository,
        FakeUserRepository userRepository,
        FakeRefreshTokenGenerator? refreshTokenGenerator = null,
        FakeAccessTokenGenerator? accessTokenGenerator = null,
        FakeUnitOfWork? unitOfWork = null,
        FakeAuthSessionRepository? authSessionRepository = null)
    {
        var sessions =
            authSessionRepository ??
            new FakeAuthSessionRepository();

        if (authSessionRepository is null)
        {
            foreach (
                var refreshToken in refreshTokenRepository.Tokens
                    .GroupBy(
                        token =>
                            token.SessionId)
                    .Select(
                        group =>
                            group.First()))
            {
                sessions.Seed(
                    AuthSession.Create(
                        refreshToken.SessionId,
                        refreshToken.UserId,
                        $"device::{refreshToken.SessionId.Value}",
                        "Test Device",
                        "127.0.0.1",
                        "PGLN.Auth.Tests",
                        Now.AddDays(-7)));
            }
        }

        return new TokenRefreshCommandHandler(
            refreshTokenRepository,
            sessions,
            userRepository,
            refreshTokenGenerator ??
                new FakeRefreshTokenGenerator
                {
                    Token =
                        "replacement-refresh-token"
                },
            accessTokenGenerator ??
                new FakeAccessTokenGenerator(),
            new FakeTokenHasher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now),
            Options);
    }

    private static User CreateUser()
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                Now.AddDays(-10));

        user.ConfirmEmail(
            Now.AddDays(-9));

        user.ClearDomainEvents();

        return user;
    }

    private static RefreshToken CreateActiveToken(
        UserId userId)
    {
        return RefreshToken.Create(
            RefreshTokenId.New(),
            RefreshTokenFamilyId.New(),
            userId,
            AuthSessionId.New(),
            "hashed::raw-refresh-token",
            Now.AddDays(-1),
            Now.AddDays(29));
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldPreserveRefreshTokenFamily()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var familyId =
            RefreshTokenFamilyId.New();

        var existing =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var result =
            await CreateHandler(
                    tokens,
                    users)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        var replacement =
            tokens.Tokens.Single(
                token =>
                    token.Id != existing.Id);

        Assert.Equal(
            familyId,
            existing.FamilyId);

        Assert.Equal(
            familyId,
            replacement.FamilyId);
    }

    [Fact]
    public async Task HandleAsync_WithReusedRotatedToken_ShouldRevokeActiveTokenFamily()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var familyId =
            RefreshTokenFamilyId.New();

        var rotatedToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-2),
                Now.AddDays(28));

        var activeReplacement =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id,
                rotatedToken.SessionId,
                "hashed::replacement-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        rotatedToken.Rotate(
            activeReplacement.Id,
            Now.AddDays(-1));

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            rotatedToken);

        repository.Seed(
            activeReplacement);

        var session =
            AuthSession.Create(
                rotatedToken.SessionId,
                user.Id,
                "device-001",
                "Test Device",
                "127.0.0.1",
                "PGLN.Auth.Tests",
                Now.AddDays(-3));

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            session);

        var unitOfWork =
            new FakeUnitOfWork();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var result =
            await CreateHandler(
                    repository,
                    users,
                    refreshTokenGenerator,
                    accessTokenGenerator,
                    unitOfWork,
                    sessions)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.True(
            activeReplacement.IsRevoked);

        Assert.Equal(
            "RefreshTokenReuseDetected",
            activeReplacement.RevocationReason);

        Assert.Equal(
            Now,
            activeReplacement.RevokedAtUtc);

        Assert.True(
            session.IsRevoked);

        Assert.Equal(
            Now,
            session.RevokedAtUtc);

        Assert.Equal(
            "RefreshTokenReuseDetected",
            session.RevocationReason);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithReusedRotatedToken_ShouldNotRevokeDifferentFamily()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var compromisedFamilyId =
            RefreshTokenFamilyId.New();

        var unrelatedFamilyId =
            RefreshTokenFamilyId.New();

        var rotatedToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                compromisedFamilyId,
                user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-2),
                Now.AddDays(28));

        var compromisedReplacement =
            RefreshToken.Create(
                RefreshTokenId.New(),
                compromisedFamilyId,
                user.Id,
                rotatedToken.SessionId,
                "hashed::compromised-replacement",
                Now.AddDays(-1),
                Now.AddDays(29));

        var unrelatedToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                unrelatedFamilyId,
                user.Id,
                rotatedToken.SessionId,
                "hashed::unrelated-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        rotatedToken.Rotate(
            compromisedReplacement.Id,
            Now.AddDays(-1));

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            rotatedToken);

        repository.Seed(
            compromisedReplacement);

        repository.Seed(
            unrelatedToken);

        var result =
            await CreateHandler(
                    repository,
                    users)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.True(
            compromisedReplacement.IsRevoked);

        Assert.Equal(
            "RefreshTokenReuseDetected",
            compromisedReplacement.RevocationReason);

        Assert.False(
            unrelatedToken.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_WithLogoutRevokedToken_ShouldNotRevokeFamily()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var familyId =
            RefreshTokenFamilyId.New();

        var loggedOutToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-2),
                Now.AddDays(28));

        var otherActiveToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id,
                loggedOutToken.SessionId,
                "hashed::other-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        loggedOutToken.Revoke(
            Now.AddMinutes(-10),
            "Logout");

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            loggedOutToken);

        repository.Seed(
            otherActiveToken);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    users,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.False(
            otherActiveToken.IsRevoked);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithReusedRotatedToken_WhenFamilyAlreadyRevoked_ShouldRevokeSession()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var familyId =
            RefreshTokenFamilyId.New();

        var rotatedToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id, AuthSessionId.New(),
                "hashed::raw-refresh-token",
                Now.AddDays(-2),
                Now.AddDays(28));

        var replacement =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id,
                rotatedToken.SessionId,
                "hashed::replacement",
                Now.AddDays(-1),
                Now.AddDays(29));

        rotatedToken.Rotate(
            replacement.Id,
            Now.AddDays(-1));

        replacement.Revoke(
            Now.AddMinutes(-5),
            "RefreshTokenReuseDetected");

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            rotatedToken);

        repository.Seed(
            replacement);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    users,
                    unitOfWork:
                        unitOfWork)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldPreserveSessionId()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessionId =
            AuthSessionId.New();

        var existing =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id,
                sessionId,
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var result =
            await CreateHandler(
                    tokens,
                    users)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        var replacement =
            tokens.Tokens.Single(
                token =>
                    token.Id != existing.Id);

        Assert.Equal(
            sessionId,
            existing.SessionId);

        Assert.Equal(
            sessionId,
            replacement.SessionId);
    }
    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldIssueAccessTokenForSameSession()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessionId =
            AuthSessionId.New();

        var existing =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id,
                sessionId,
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    accessTokenGenerator:
                        accessTokenGenerator)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            sessionId,
            accessTokenGenerator.LastSessionId);

        Assert.Equal(
            user,
            accessTokenGenerator.LastUser);

        Assert.Equal(
            Now,
            accessTokenGenerator.LastIssuedAtUtc);
    }
    [Fact]
    public async Task HandleAsync_WhenSessionIsRevoked_ShouldReturnRevokedToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessionId =
            AuthSessionId.New();

        var session =
            AuthSession.Create(
                sessionId,
                user.Id,
                "device-001",
                "Test Device",
                "127.0.0.1",
                "PGLN.Auth.Tests",
                Now.AddDays(-2));

        session.Revoke(
            Now.AddMinutes(-5),
            "Email address changed.");

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            session);

        var existing =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id,
                sessionId,
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    unitOfWork:
                        unitOfWork,
                    authSessionRepository:
                        sessions)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.Single(
            tokens.Tokens);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionDoesNotExist_ShouldReturnRevokedToken()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessionId =
            AuthSessionId.New();

        var existing =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id,
                sessionId,
                "hashed::raw-refresh-token",
                Now.AddDays(-1),
                Now.AddDays(29));

        var tokens =
            new FakeRefreshTokenRepository();

        tokens.Seed(
            existing);

        var sessions =
            new FakeAuthSessionRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    tokens,
                    users,
                    unitOfWork:
                        unitOfWork,
                    authSessionRepository:
                        sessions)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.Single(
            tokens.Tokens);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithReusedRotatedToken_WhenFamilyAndSessionAlreadyRevoked_ShouldNotSaveAgain()
    {
        var user =
            CreateUser();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var familyId =
            RefreshTokenFamilyId.New();

        var sessionId =
            AuthSessionId.New();

        var rotatedToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id,
                sessionId,
                "hashed::raw-refresh-token",
                Now.AddDays(-2),
                Now.AddDays(28));

        var replacement =
            RefreshToken.Create(
                RefreshTokenId.New(),
                familyId,
                user.Id,
                sessionId,
                "hashed::replacement",
                Now.AddDays(-1),
                Now.AddDays(29));

        rotatedToken.Rotate(
            replacement.Id,
            Now.AddDays(-1));

        replacement.Revoke(
            Now.AddMinutes(-5),
            "RefreshTokenReuseDetected");

        var session =
            AuthSession.Create(
                sessionId,
                user.Id,
                "device-001",
                "Test Device",
                "127.0.0.1",
                "PGLN.Auth.Tests",
                Now.AddDays(-3));

        session.Revoke(
            Now.AddMinutes(-5),
            "RefreshTokenReuseDetected");

        var repository =
            new FakeRefreshTokenRepository();

        repository.Seed(
            rotatedToken);

        repository.Seed(
            replacement);

        var sessions =
            new FakeAuthSessionRepository();

        sessions.Seed(
            session);

        var unitOfWork =
            new FakeUnitOfWork();

        var result =
            await CreateHandler(
                    repository,
                    users,
                    unitOfWork:
                        unitOfWork,
                    authSessionRepository:
                        sessions)
                .HandleAsync(
                    new TokenRefreshCommand(
                        "raw-refresh-token"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TokenRefreshErrors.RevokedToken,
            result.Error);

        Assert.True(
            session.IsRevoked);

        Assert.True(
            replacement.IsRevoked);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }}



















