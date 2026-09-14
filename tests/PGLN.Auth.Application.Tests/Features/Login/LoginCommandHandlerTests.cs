using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.Application.Events.Email;
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
                new LoginCommand("missing@example.com", "Password123!", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

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
                passwordHasher: passwordHasher);

        var result =
            await handler.HandleAsync(
                new LoginCommand("USER@example.com", "wrong-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

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
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

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

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Test Device",
                Now));

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
                refreshTokenGenerator,
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

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

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Test Device",
                Now));

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
                generator,
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

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

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Test Device",
                Now));

        var attempts =
            new FakeLoginAttemptRepository();

        var handler =
            CreateHandler(
                users,
                attempts,
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand("USER@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

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
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenMaximumFailedAttemptsReached_ShouldLockAccount()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var loginAttempts =
            new FakeLoginAttemptRepository();

        var integrationEventPublisher =
            new FakeIntegrationEventPublisher();

        var handler =
            CreateHandler(
                users,
                loginAttempts,
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                integrationEventPublisher:
                    integrationEventPublisher,
                accountLockoutOptions:
                    new AccountLockoutOptions
                    {
                        MaxFailedAttempts = 3,
                        FailureWindow =
                            TimeSpan.FromMinutes(15),
                        LockoutDuration =
                            TimeSpan.FromMinutes(10)
                    });

        for (var attempt = 0;
             attempt < 3;
             attempt++)
        {
            var result =
                await handler.HandleAsync(
                    new LoginCommand("user@example.com", "wrong-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

            Assert.True(
                result.IsFailure);
        }

        Assert.Equal(
            3,
            user.FailedLoginAttempts);

        Assert.NotNull(
            user.LockoutEndUtc);

        Assert.Equal(
            Now.AddMinutes(10),
            user.LockoutEndUtc);

        Assert.True(
            user.IsLockedOut(
                Now));

        var integrationEvent =
            Assert.Single(
                integrationEventPublisher.Events);

        var accountLocked =
            Assert.IsType<AccountLockedNotificationRequested>(
                integrationEvent);

        Assert.Equal(
            user.Id,
            accountLocked.UserId);

        Assert.Equal(
            "user@example.com",
            accountLocked.Email);

        Assert.Equal(
            Now.AddMinutes(10),
            accountLocked.LockedUntilUtc);

        Assert.Equal(
            Now,
            accountLocked.OccurredAtUtc);
    }
    [Fact]
    public async Task HandleAsync_WhenPreviousFailureIsOutsideFailureWindow_ShouldResetCounter()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-30));

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-29));

        users.Seed(
            user);

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                accountLockoutOptions:
                    new AccountLockoutOptions
                    {
                        MaxFailedAttempts = 3,
                        FailureWindow =
                            TimeSpan.FromMinutes(15),
                        LockoutDuration =
                            TimeSpan.FromMinutes(10)
                    });

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "wrong-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            1,
            user.FailedLoginAttempts);

        Assert.Equal(
            Now,
            user.LastFailedLoginAtUtc);

        Assert.Null(
            user.LockoutEndUtc);
    }
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldResetFailedLoginState()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-5));

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-4));

        users.Seed(
            user);

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator());

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            user.FailedLoginAttempts);

        Assert.Null(
            user.LastFailedLoginAtUtc);

        Assert.Null(
            user.LockoutEndUtc);
    }
    [Fact]
    public async Task HandleAsync_WhenAccountIsLocked_ShouldRejectLogin()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        user.LockOutUntil(
            Now.AddMinutes(10));

        users.Seed(
            user);

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator());

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.AccountLocked,
            result.Error);

        Assert.True(
            user.IsLockedOut(
                Now));
    }
    [Fact]
    public async Task HandleAsync_WhenLockoutHasExpired_ShouldAllowValidLogin()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-20));

        user.RecordFailedLoginAttempt(
            Now.AddMinutes(-19));

        user.LockOutUntil(
            Now.AddMinutes(-1));

        users.Seed(
            user);

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator());

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            0,
            user.FailedLoginAttempts);

        Assert.Null(
            user.LastFailedLoginAtUtc);

        Assert.Null(
            user.LockoutEndUtc);
    }
    [Fact]
    public async Task HandleAsync_WhenEmailThrottleLimitReached_ShouldReturnTooManyAttempts()
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

        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            await attempts.AddAsync(
                LoginAttempt.Failed(
                    user.Email.NormalizedValue,
                    user.Id,
                    LoginFailureReason.InvalidCredentials,
                    Now.AddMinutes(-1)));
        }

        var passwordHasher =
            new FakeLoginPasswordHasher();

        var accessTokens =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var handler =
            CreateHandler(
                users,
                attempts,
                refreshTokens,
                accessTokens,
                refreshTokenGenerator,
                passwordHasher: passwordHasher,
                loginEmailThrottleOptions:
                    new LoginEmailThrottleOptions
                    {
                        MaxFailedAttempts = 10,
                        Window =
                            TimeSpan.FromMinutes(5)
                    });

        var result =
            await handler.HandleAsync(
                new LoginCommand("USER@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.TooManyAttempts,
            result.Error);

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
    public async Task HandleAsync_WhenPreviousFailuresAreOutsideEmailThrottleWindow_ShouldAllowLogin()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Test Device",
                Now));

        var attempts =
            new FakeLoginAttemptRepository();

        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            await attempts.AddAsync(
                LoginAttempt.Failed(
                    user.Email.NormalizedValue,
                    user.Id,
                    LoginFailureReason.InvalidCredentials,
                    Now.AddMinutes(-10)));
        }

        var accessTokens =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var handler =
            CreateHandler(
                users,
                attempts,
                new FakeRefreshTokenRepository(),
                accessTokens,
                refreshTokenGenerator,
                loginEmailThrottleOptions:
                    new LoginEmailThrottleOptions
                    {
                        MaxFailedAttempts = 10,
                        Window =
                            TimeSpan.FromMinutes(5)
                    },
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            accessTokens.GenerateCallCount);

        Assert.Equal(
            1,
            refreshTokenGenerator.GenerateCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenAnotherEmailHasReachedThrottleLimit_ShouldAllowLogin()
    {
        var users =
            new FakeUserRepository();

        var user =
            CreateUser(
                confirmed: true);

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Test Device",
                Now));

        var attempts =
            new FakeLoginAttemptRepository();

        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            await attempts.AddAsync(
                LoginAttempt.Failed(
                    "OTHER@EXAMPLE.COM",
                    null,
                    LoginFailureReason.InvalidCredentials,
                    Now.AddMinutes(-1)));
        }

        var accessTokens =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var handler =
            CreateHandler(
                users,
                attempts,
                new FakeRefreshTokenRepository(),
                accessTokens,
                refreshTokenGenerator,
                loginEmailThrottleOptions:
                    new LoginEmailThrottleOptions
                    {
                        MaxFailedAttempts = 10,
                        Window =
                            TimeSpan.FromMinutes(5)
                    },
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            1,
            accessTokens.GenerateCallCount);

        Assert.Equal(
            1,
            refreshTokenGenerator.GenerateCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenSameEmailUsesDifferentCasing_ShouldShareThrottleBucket()
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

        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            await attempts.AddAsync(
                LoginAttempt.Failed(
                    "USER@EXAMPLE.COM",
                    user.Id,
                    LoginFailureReason.InvalidCredentials,
                    Now.AddMinutes(-1)));
        }

        var handler =
            CreateHandler(
                users,
                attempts,
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                loginEmailThrottleOptions:
                    new LoginEmailThrottleOptions
                    {
                        MaxFailedAttempts = 10,
                        Window =
                            TimeSpan.FromMinutes(5)
                    });

        var result =
            await handler.HandleAsync(
                new LoginCommand("user@example.com", "correct-password", "device-hash-001", "Test Device", "127.0.0.1", "TestAgent/1.0"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.TooManyAttempts,
            result.Error);
    }
        [Fact]
    public async Task HandleAsync_WhenLoggingInFromNewDevice_ShouldRequireStepUpVerification()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var integrationEvents =
            new FakeIntegrationEventPublisher();

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                new FakeRefreshTokenRepository(),
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                authSessionRepository: sessions,
                integrationEventPublisher: integrationEvents);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    user.Email.Value,
                    "correct-password",
                    "device-hash-new",
                    "Firefox on Linux",
                    "192.168.1.25",
                    "Firefox/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.StepUpRequired,
            result.Value.Status);

        Assert.Null(
            result.Value.AccessToken);

        Assert.Null(
            result.Value.RefreshToken);

        Assert.NotNull(
            result.Value.StepUpChallengeId);

        Assert.Empty(
            sessions.Sessions);

        var integrationEvent =
            Assert.Single(
                integrationEvents.Events);

        var verificationRequest =
            Assert.IsType<StepUpVerificationCodeRequested>(
                integrationEvent);

        Assert.Equal(
            user.Id,
            verificationRequest.UserId);

        Assert.Equal(
            user.Email.Value,
            verificationRequest.Email);

        Assert.Equal(
            "Firefox on Linux",
            verificationRequest.DeviceName);

        Assert.Equal(
            "192.168.1.25",
            verificationRequest.IpAddress);

        Assert.Equal(
            "Firefox/1.0",
            verificationRequest.UserAgent);

        Assert.Equal(
            Now,
            verificationRequest.OccurredAtUtc);
    }
        [Fact]
    public async Task HandleAsync_WhenDeviceAlreadyHasActiveSessionButIsNotTrusted_ShouldRequireStepUp()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var existingSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-known",
                "Chrome on Windows",
                "10.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-2));

        sessions.Seed(
            existingSession);

        var integrationEvents =
            new FakeIntegrationEventPublisher();

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    new FakeRefreshTokenRepository(),
                    new FakeAccessTokenGenerator(),
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository: sessions,
                    integrationEventPublisher: integrationEvents)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-known",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "Chrome/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.StepUpRequired,
            result.Value.Status);

        Assert.Null(
            result.Value.AccessToken);

        Assert.Null(
            result.Value.RefreshToken);

        Assert.Single(
            sessions.Sessions);

        Assert.Equal(
            existingSession.Id,
            sessions.Sessions.Single().Id);

        var integrationEvent =
            Assert.Single(
                integrationEvents.Events);

        Assert.IsType<StepUpVerificationCodeRequested>(
            integrationEvent);
    }
        [Fact]
    public async Task HandleAsync_WhenPreviouslySeenDeviceHasRevokedSessionAndIsNotTrusted_ShouldRequireStepUp()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var revokedSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-known",
                "Chrome on Windows",
                "10.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-5));

        revokedSession.Revoke(
            Now.AddDays(-1),
            "UserLoggedOut");

        sessions.Seed(
            revokedSession);

        var integrationEvents =
            new FakeIntegrationEventPublisher();

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    new FakeRefreshTokenRepository(),
                    new FakeAccessTokenGenerator(),
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository: sessions,
                    integrationEventPublisher: integrationEvents)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-known",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "Chrome/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.StepUpRequired,
            result.Value.Status);

        Assert.Null(
            result.Value.AccessToken);

        Assert.Null(
            result.Value.RefreshToken);

        Assert.Single(
            sessions.Sessions);

        Assert.Equal(
            revokedSession.Id,
            sessions.Sessions.Single().Id);

        var integrationEvent =
            Assert.Single(
                integrationEvents.Events);

        Assert.IsType<StepUpVerificationCodeRequested>(
            integrationEvent);
    }
    [Fact]
    public async Task HandleAsync_WhenDeviceWasPreviouslyTrusted_ShouldTrustNewSession()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var revokedSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-trusted",
                "Chrome on Windows",
                "10.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-5));

        revokedSession.Revoke(
            Now.AddDays(-1),
            "UserLoggedOut");

        sessions.Seed(
            revokedSession);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        var trustedDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-trusted",
                "Chrome on Windows",
                Now.AddDays(-10));

        trustedDevices.Seed(
            trustedDevice);

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    new FakeRefreshTokenRepository(),
                    new FakeAccessTokenGenerator(),
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository: sessions,
                    trustedDeviceRepository: trustedDevices)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-trusted",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "Chrome/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            2,
            sessions.Sessions.Count);

        var newSession =
            sessions.Sessions.Single(
                session =>
                    session.Id != revokedSession.Id);

        Assert.True(
            newSession.IsTrustedDevice);

        Assert.Equal(
            DeviceTrustStatus.Trusted,
            newSession.DeviceTrustStatus);
    }
        [Fact]
    public async Task HandleAsync_WhenTrustedDeviceWasRevoked_ShouldRequireStepUpWithoutCreatingSession()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var revokedSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-revoked",
                "Chrome on Windows",
                "10.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-5));

        revokedSession.Revoke(
            Now.AddDays(-1),
            "UserLoggedOut");

        sessions.Seed(
            revokedSession);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        var trustedDevice =
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-revoked",
                "Chrome on Windows",
                Now.AddDays(-10));

        trustedDevice.Revoke(
            Now.AddDays(-2),
            "UserRevokedTrust");

        trustedDevices.Seed(
            trustedDevice);

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    new FakeRefreshTokenRepository(),
                    new FakeAccessTokenGenerator(),
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository: sessions,
                    trustedDeviceRepository: trustedDevices)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-revoked",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "Chrome/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.StepUpRequired,
            result.Value.Status);

        Assert.Null(
            result.Value.AccessToken);

        Assert.Null(
            result.Value.RefreshToken);

        Assert.NotNull(
            result.Value.StepUpChallengeId);

        Assert.Single(
            sessions.Sessions);

        Assert.Equal(
            revokedSession.Id,
            sessions.Sessions.Single().Id);
    }
        [Fact]
    public async Task HandleAsync_WhenDeviceIsNotTrusted_ShouldRequireStepUpWithoutCreatingSession()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    new FakeRefreshTokenRepository(),
                    new FakeAccessTokenGenerator(),
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository: sessions,
                    trustedDeviceRepository:
                        new FakeTrustedDeviceRepository())
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-untrusted",
                        "Safari on iPhone",
                        "192.168.1.50",
                        "Safari/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.StepUpRequired,
            result.Value.Status);

        Assert.Null(
            result.Value.AccessToken);

        Assert.Null(
            result.Value.RefreshToken);

        Assert.NotNull(
            result.Value.StepUpChallengeId);

        Assert.Empty(
            sessions.Sessions);
    }
    [Fact]
    public async Task HandleAsync_WhenDeviceIsUntrusted_ShouldRequireStepUpWithoutIssuingCredentials()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var sessions =
            new FakeAuthSessionRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var challenges =
            new FakeStepUpChallengeRepository();

        var codeGenerator =
            new FakeStepUpCodeGenerator();

        var codeProtector =
            new FakeStepUpCodeProtector();

        var integrationEvents =
            new FakeIntegrationEventPublisher();

        var loginAttempts =
            new FakeLoginAttemptRepository();

        var result =
            await CreateHandler(
                    users,
                    loginAttempts,
                    refreshTokens,
                    accessTokenGenerator,
                    refreshTokenGenerator,
                    authSessionRepository:
                        sessions,
                    trustedDeviceRepository:
                        new FakeTrustedDeviceRepository(),
                    stepUpChallengeRepository:
                        challenges,
                    stepUpCodeGenerator:
                        codeGenerator,
                    stepUpCodeProtector:
                        codeProtector,
                    integrationEventPublisher:
                        integrationEvents)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-untrusted",
                        "Firefox on Linux",
                        "192.168.1.25",
                        "Firefox/1.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.StepUpRequired,
            result.Value.Status);

        Assert.Equal(
            user.Id.Value,
            result.Value.UserId);

        Assert.Equal(
            user.Email.Value,
            result.Value.Email);

        Assert.Null(
            result.Value.AccessToken);

        Assert.Null(
            result.Value.AccessTokenExpiresAtUtc);

        Assert.Null(
            result.Value.RefreshToken);

        Assert.Null(
            result.Value.RefreshTokenExpiresAtUtc);

        Assert.NotNull(
            result.Value.StepUpChallengeId);

        Assert.Empty(
            sessions.Sessions);

        Assert.Empty(
            refreshTokens.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        Assert.Empty(
            loginAttempts.Attempts);

        var challenge =
            Assert.Single(
                challenges.Challenges);

        Assert.Equal(
            result.Value.StepUpChallengeId,
            challenge.Id.Value);

        Assert.Equal(
            user.Id,
            challenge.UserId);

        Assert.Equal(
            "device-hash-untrusted",
            challenge.DeviceIdHash);

        Assert.Equal(
            "protected::123456",
            challenge.CodeHash);

        Assert.Equal(
            1,
            codeGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            codeProtector.ProtectCallCount);

        var integrationEvent =
            Assert.Single(
                integrationEvents.Events);

        var verificationRequest =
            Assert.IsType<StepUpVerificationCodeRequested>(
                integrationEvent);

        Assert.Equal(
            challenge.Id,
            verificationRequest.ChallengeId);

        Assert.Equal(
            "123456",
            verificationRequest.Code);

        Assert.Equal(
            user.Email.Value,
            verificationRequest.Email);
    }
    [Fact]
    public async Task HandleAsync_WhenDeviceIsTrusted_ShouldCompleteAuthenticationAndIssueCredentials()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-trusted",
                "Chrome on Windows",
                Now));

        var sessions =
            new FakeAuthSessionRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    refreshTokens,
                    accessTokenGenerator,
                    refreshTokenGenerator,
                    authSessionRepository:
                        sessions,
                    trustedDeviceRepository:
                        trustedDevices)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-trusted",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "Chrome/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            LoginStatus.AuthenticationComplete,
            result.Value.Status);

        Assert.Equal(
            user.Id.Value,
            result.Value.UserId);

        Assert.Equal(
            user.Email.Value,
            result.Value.Email);

        Assert.Equal(
            "fake-access-token",
            result.Value.AccessToken);

        Assert.NotNull(
            result.Value.AccessTokenExpiresAtUtc);

        Assert.Equal(
            "raw-refresh-token",
            result.Value.RefreshToken);

        Assert.NotNull(
            result.Value.RefreshTokenExpiresAtUtc);

        Assert.Null(
            result.Value.StepUpChallengeId);

        var session =
            Assert.Single(
                sessions.Sessions);

        Assert.Equal(
            user.Id,
            session.UserId);

        Assert.Equal(
            "device-hash-trusted",
            session.DeviceIdHash);

        Assert.True(
            session.IsTrustedDevice);

        Assert.Single(
            refreshTokens.Tokens);

        Assert.Equal(
            1,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            1,
            refreshTokenGenerator.GenerateCallCount);
    }
    [Fact]
    public async Task HandleAsync_WhenUserHasNoPassword_ShouldReturnInvalidCredentials()
    {
        var user =
            User.RegisterExternal(
                UserId.New(),
                Email.Create(
                    "external@example.com"),
                Now.AddDays(-10));

        user.ClearDomainEvents();

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var loginAttempts =
            new FakeLoginAttemptRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var sessions =
            new FakeAuthSessionRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokenGenerator =
            new FakeRefreshTokenGenerator();

        var result =
            await CreateHandler(
                    users,
                    loginAttempts,
                    refreshTokens,
                    accessTokenGenerator,
                    refreshTokenGenerator,
                    authSessionRepository:
                        sessions)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "some-password",
                        "external-device",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "TestAgent/1.0"));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            LoginErrors.InvalidCredentials,
            result.Error);

        Assert.Empty(
            sessions.Sessions);

        Assert.Empty(
            refreshTokens.Tokens);

        Assert.Equal(
            0,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            0,
            refreshTokenGenerator.GenerateCallCount);

        var attempt =
            Assert.Single(
                loginAttempts.Attempts);

        Assert.False(
            attempt.Succeeded);

        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            attempt.FailureReason);
    }
    private static LoginCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeLoginAttemptRepository loginAttemptRepository,
        FakeRefreshTokenRepository refreshTokenRepository,
        FakeAccessTokenGenerator accessTokenGenerator,
        FakeRefreshTokenGenerator refreshTokenGenerator,
        FakeAuthSessionRepository? authSessionRepository = null,
        FakeTrustedDeviceRepository? trustedDeviceRepository = null,
        FakeStepUpChallengeRepository? stepUpChallengeRepository = null,
        FakeStepUpCodeGenerator? stepUpCodeGenerator = null,
        FakeStepUpCodeProtector? stepUpCodeProtector = null,
        FakeLoginPasswordHasher? passwordHasher = null,
        FakeIntegrationEventPublisher? integrationEventPublisher = null,
        FakeUnitOfWork? unitOfWork = null,
        AccountLockoutOptions? accountLockoutOptions = null,
        LoginEmailThrottleOptions? loginEmailThrottleOptions = null,
        StepUpChallengeOptions? stepUpChallengeOptions = null)
    {
        return new LoginCommandHandler(
            userRepository,
            loginAttemptRepository,
            refreshTokenRepository,
            authSessionRepository ??
                new FakeAuthSessionRepository(),
            trustedDeviceRepository ??
                new FakeTrustedDeviceRepository(),
            stepUpChallengeRepository ??
                new FakeStepUpChallengeRepository(),
            stepUpCodeGenerator ??
                new FakeStepUpCodeGenerator(),
            stepUpCodeProtector ??
                new FakeStepUpCodeProtector(),
            passwordHasher ??
                new FakeLoginPasswordHasher(),
            accessTokenGenerator,
            refreshTokenGenerator,
            new FakeTokenHasher(),
            integrationEventPublisher ??
                new FakeIntegrationEventPublisher(),
            unitOfWork ??
                new FakeUnitOfWork(),
            new FakeClock(
                Now),
            RefreshOptions,
            accountLockoutOptions ??
                new AccountLockoutOptions(),
            loginEmailThrottleOptions ??
                new LoginEmailThrottleOptions(),
            stepUpChallengeOptions ??
                new StepUpChallengeOptions());
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

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldCreateSessionForDevice()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Test Device",
                Now));

        var sessions =
            new FakeAuthSessionRepository();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    refreshTokens,
                    new FakeAccessTokenGenerator(),
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository: sessions,
                    trustedDeviceRepository:
                        trustedDevices)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-001",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        var session =
            Assert.Single(
                sessions.Sessions);

        Assert.Equal(
            user.Id,
            session.UserId);

        Assert.Equal(
            "device-hash-001",
            session.DeviceIdHash);

        Assert.Equal(
            "Chrome on Windows",
            session.DeviceName);

        Assert.Equal(
            "127.0.0.1",
            session.IpAddress);

        Assert.Equal(
            "TestAgent/1.0",
            session.UserAgent);

        var refreshToken =
            Assert.Single(
                refreshTokens.Tokens);

        Assert.Equal(
            session.Id,
            refreshToken.SessionId);
    }
    [Fact]
    public async Task HandleAsync_WhenDeviceAlreadyHasActiveSession_ShouldReuseExistingSession()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Chrome on Windows",
                Now));

        var sessions =
            new FakeAuthSessionRepository();

        var existingSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-001",
                "Chrome on Windows",
                "10.0.0.1",
                "OldAgent/1.0",
                Now.AddDays(-2));

        sessions.Seed(
            existingSession);

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                refreshTokens,
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                authSessionRepository: sessions,
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    user.Email.Value,
                    "correct-password",
                    "device-hash-001",
                    "Chrome on Windows",
                    "127.0.0.1",
                    "NewAgent/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Single(
            sessions.Sessions);

        var session =
            sessions.Sessions.Single();

        Assert.Equal(
            existingSession.Id,
            session.Id);

        Assert.Equal(
            "127.0.0.1",
            session.IpAddress);

        Assert.Equal(
            "NewAgent/2.0",
            session.UserAgent);

        Assert.Equal(
            Now,
            session.LastSeenAtUtc);

        var refreshToken =
            Assert.Single(
                refreshTokens.Tokens);

        Assert.Equal(
            existingSession.Id,
            refreshToken.SessionId);
    }
    [Fact]
    public async Task HandleAsync_WhenLoggingInFromDifferentDevice_ShouldCreateNewSession()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-002",
                "Safari on iPhone",
                Now));

        var sessions =
            new FakeAuthSessionRepository();

        var existingSession =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-001",
                "Chrome on Windows",
                "10.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-2));

        sessions.Seed(
            existingSession);

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var handler =
            CreateHandler(
                users,
                new FakeLoginAttemptRepository(),
                refreshTokens,
                new FakeAccessTokenGenerator(),
                new FakeRefreshTokenGenerator(),
                authSessionRepository: sessions,
                trustedDeviceRepository:
                    trustedDevices);

        var result =
            await handler.HandleAsync(
                new LoginCommand(
                    user.Email.Value,
                    "correct-password",
                    "device-hash-002",
                    "Safari on iPhone",
                    "192.168.1.10",
                    "Safari/2.0"));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            2,
            sessions.Sessions.Count);

        var newSession =
            sessions.Sessions.Single(
                session =>
                    session.DeviceIdHash == "device-hash-002");

        Assert.NotEqual(
            existingSession.Id,
            newSession.Id);

        Assert.Equal(
            user.Id,
            newSession.UserId);

        Assert.Equal(
            "Safari on iPhone",
            newSession.DeviceName);

        Assert.Equal(
            "192.168.1.10",
            newSession.IpAddress);

        Assert.Equal(
            "Safari/2.0",
            newSession.UserAgent);

        var refreshToken =
            Assert.Single(
                refreshTokens.Tokens);

        Assert.Equal(
            newSession.Id,
            refreshToken.SessionId);
    }
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldIssueAccessTokenForCreatedSession()
    {
        var user =
            CreateUser(
                confirmed: true);

        var users =
            new FakeUserRepository();

        users.Seed(
            user);

        var trustedDevices =
            new FakeTrustedDeviceRepository();

        trustedDevices.Seed(
            TrustedDevice.Create(
                TrustedDeviceId.New(),
                user.Id,
                "device-hash-001",
                "Chrome on Windows",
                Now));

        var sessions =
            new FakeAuthSessionRepository();

        var accessTokenGenerator =
            new FakeAccessTokenGenerator();

        var refreshTokens =
            new FakeRefreshTokenRepository();

        var result =
            await CreateHandler(
                    users,
                    new FakeLoginAttemptRepository(),
                    refreshTokens,
                    accessTokenGenerator,
                    new FakeRefreshTokenGenerator(),
                    authSessionRepository:
                        sessions,
                    trustedDeviceRepository:
                        trustedDevices)
                .HandleAsync(
                    new LoginCommand(
                        user.Email.Value,
                        "correct-password",
                        "device-hash-001",
                        "Chrome on Windows",
                        "127.0.0.1",
                        "TestAgent/1.0"));

        Assert.True(
            result.IsSuccess);

        var session =
            Assert.Single(
                sessions.Sessions);

        Assert.Equal(
            1,
            accessTokenGenerator.GenerateCallCount);

        Assert.Equal(
            session.Id,
            accessTokenGenerator.LastSessionId);

        Assert.Equal(
            user,
            accessTokenGenerator.LastUser);

        Assert.Equal(
            Now,
            accessTokenGenerator.LastIssuedAtUtc);

        var refreshToken =
            Assert.Single(
                refreshTokens.Tokens);

        Assert.Equal(
            session.Id,
            refreshToken.SessionId);

        Assert.Equal(
            refreshToken.SessionId,
            accessTokenGenerator.LastSessionId);
    }}





















































