using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.TokenRefresh;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.RefreshTokens;
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
                user.Id,
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
    public async Task HandleAsync_WithRevokedToken_ShouldReturnRevokedToken()
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
                user.Id,
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
            0,
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
                missingUserId,
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
        FakeUnitOfWork? unitOfWork = null)
    {
        return new TokenRefreshCommandHandler(
            refreshTokenRepository,
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
            userId,
            "hashed::raw-refresh-token",
            Now.AddDays(-1),
            Now.AddDays(29));
    }
}
