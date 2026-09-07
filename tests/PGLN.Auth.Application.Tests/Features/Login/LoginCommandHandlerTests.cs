using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.Login;

public sealed class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            7,
            18,
            30,
            0,
            TimeSpan.Zero);

    private static readonly RefreshTokenOptions RefreshOptions =
        new()
        {
            TokenLifetime =
                TimeSpan.FromDays(30)
        };

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnInvalidCredentials()
    {
        var attempts =
            new FakeLoginAttemptRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var accessTokens =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                new FakeUserRepository(),
                attempts,
                refreshTokens,
                accessTokens,
                refreshTokenGenerator,
                unitOfWork: unitOfWork);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "missing@example.com",
                    "Password123!"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.InvalidCredentials,
            result.Error);

        var attempt =
            Assert.Single(
                attempts.Attempts);

        Assert.False(
            attempt.Succeeded);

        Assert.Null(
            attempt.UserId);

        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            attempt.FailureReason);

        Assert.Empty(
            refreshTokens.Tokens);

        Assert.Equal(
            0,
            accessTokens.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenPasswordIsWrong_ShouldReturnInvalidCredentials()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var passwordHasher =
            new FakeLoginPasswordHasher();

        var attempts =
            new FakeLoginAttemptRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var accessTokens =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var handler =
            CreateHandler(
                users,
                attempts,
                refreshTokens,
                accessTokens,
                refreshTokenGenerator,
                passwordHasher);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "USER@example.com",
                    "wrong-password"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.InvalidCredentials,
            result.Error);

        var attempt =
            Assert.Single(
                attempts.Attempts);

        Assert.Equal(
            user.Id,
            attempt.UserId);

        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            attempt.FailureReason);

        Assert.Empty(
            refreshTokens.Tokens);

        Assert.Equal(
            0,
            accessTokens.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsNotConfirmed_ShouldNotIssueTokens()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: false);

        users.Seed(
            user);

        var attempts =
            new FakeLoginAttemptRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var accessTokens =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var handler =
            CreateHandler(
                users,
                attempts,
                refreshTokens,
                accessTokens,
                refreshTokenGenerator);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.EmailNotConfirmed,
            result.Error);

        var attempt =
            Assert.Single(
                attempts.Attempts);

        Assert.False(
            attempt.Succeeded);

        Assert.Equal(
            user.Id,
            attempt.UserId);

        Assert.Equal(
            LoginFailureReason.EmailNotConfirmed,
            attempt.FailureReason);

        Assert.Empty(
            refreshTokens.Tokens);

        Assert.Equal(
            0,
            accessTokens.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldReturnTokens()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var accessTokens =
            new FakeAccessTokenGenerator
            {
                Token =
                    "access-token"
            };

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator
            {
                Token =
                    "raw-refresh-token"
            };

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                accessTokens,
                refreshTokenGenerator);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            user.Id.Value,
            result.Value.UserId);

        Assert.Equal(
            "user@example.com",
            result.Value.Email);

        Assert.Equal(
            "access-token",
            result.Value.AccessToken);

        Assert.Equal(
            "raw-refresh-token",
            result.Value.RefreshToken);

        Assert.Equal(
            Now.AddMinutes(15),
            result.Value.AccessTokenExpiresAtUtc);

        Assert.Equal(
            Now.AddDays(30),
            result.Value.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldPersistOnlyRefreshTokenHash()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var repository =
            new FakeRefreshTokenRepository();

        var generator =
            new FakeRefreshTokenGenerator
            {
                Token =
                    "super-secret-refresh-token"
            };

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                repository,
                new FakeAccessTokenGenerator(),
                generator);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.True(
            result.IsSuccess);

        var persistedToken =
            Assert.Single(
                repository.Tokens);

        Assert.Equal(
            "hashed::super-secret-refresh-token",
            persistedToken.TokenHash);

        Assert.NotEqual(
            "super-secret-refresh-token",
            persistedToken.TokenHash);

        Assert.Equal(
            user.Id,
            persistedToken.UserId);

        Assert.Equal(
            Now,
            persistedToken.CreatedAtUtc);

        Assert.Equal(
            Now.AddDays(30),
            persistedToken.ExpiresAtUtc);

        Assert.Equal(
            "super-secret-refresh-token",
            result.Value.RefreshToken);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldRecordSuccessfulLoginAttempt()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var attempts =
            new FakeLoginAttemptRepository();

        var handler =
            CreateHandler(
                users,
                attempts,
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator());

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "USER@example.com",
                    "correct-password"));

        Assert.True(
            result.IsSuccess);

        var attempt =
            Assert.Single(
                attempts.Attempts);

        Assert.True(
            attempt.Succeeded);

        Assert.Equal(
            user.Id,
            attempt.UserId);

        Assert.Null(
            attempt.FailureReason);

        Assert.Equal(
            Now,
            attempt.AttemptedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldSaveChangesOnce()
    {
        var users =
            new FakeUserRepository();

        users.Seed(
            CreateUser(
                confirmed: true));

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                unitOfWork: unitOfWork);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    private static LoginCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeLoginAttemptRepository loginAttemptRepository,
        FakeRefreshTokenRepository refreshTokenRepository,
        FakeAccessTokenGenerator accessTokenGenerator,
        FakeRefreshTokenGenerator refreshTokenGenerator,
        FakeLoginPasswordHasher? passwordHasher = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new LoginCommandHandler(
            userRepository,
            loginAttemptRepository,
            refreshTokenRepository,
            passwordHasher ??
                new FakeLoginPasswordHasher(),
            accessTokenGenerator,
            refreshTokenGenerator,
            new FakeTokenHasher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now),
            RefreshOptions);
    }

    private static User CreateUser(
        bool confirmed)
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed::correct-password",
                Now.AddDays(-10));

        user.ClearDomainEvents();

        if (confirmed)
        {
            user.ConfirmEmail(
                Now.AddDays(-9));

            user.ClearDomainEvents();
        }

        return user;
    }
}

