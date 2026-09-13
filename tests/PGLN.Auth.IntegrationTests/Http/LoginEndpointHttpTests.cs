using PGLN.Auth.Application.Features.Login;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.AspNetCore.RateLimiting;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class LoginEndpointHttpTests
{
    [Fact]
    public async Task Login_WithValidConfirmedUserOnUntrustedDevice_ShouldRequireStepUp()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string password =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            "user@example.com",
            password,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    password,
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            body);

        Assert.Equal(
            "StepUpRequired",
            body.Status);

        Assert.Equal(
            "user@example.com",
            body.Email);

        Assert.NotEqual(
            Guid.Empty,
            body.UserId);

        Assert.NotNull(
            body.StepUpChallengeId);

        Assert.NotEqual(
            Guid.Empty,
            body.StepUpChallengeId.Value);

        Assert.Null(
            body.AccessToken);

        Assert.Null(
            body.AccessTokenExpiresAtUtc);

        Assert.Null(
            body.RefreshToken);

        Assert.Null(
            body.RefreshTokenExpiresAtUtc);
    }
    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedUserAsync(
            application,
            "user@example.com",
            "SecretPassword123!",
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    "WrongPassword123!",
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnSame401AsWrongPassword()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "Login.InvalidCredentials",
            payload);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ShouldReturn403()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedUserAsync(
            application,
            "user@example.com",
            "SecretPassword123!",
            confirmed:
                false);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    "SecretPassword123!",
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Login_WithValidCredentialsOnUntrustedDevice_ShouldPersistStepUpChallengeWithoutSessionOrTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string password =
            "SecretPassword123!";

        var user =
            await SeedUserAsync(
                application,
                "user@example.com",
                password,
                confirmed:
                    true);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    password,
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            body);

        Assert.Equal(
            "StepUpRequired",
            body.Status);

        Assert.NotNull(
            body.StepUpChallengeId);

        Assert.Null(
            body.AccessToken);

        Assert.Null(
            body.RefreshToken);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        Assert.Empty(
            await dbContext.LoginAttempts
                .ToListAsync());

        var challenge =
            await dbContext.StepUpChallenges
                .SingleAsync();

        Assert.Equal(
            user.Id,
            challenge.UserId);

        Assert.Equal(
            body.StepUpChallengeId.Value,
            challenge.Id.Value);

        Assert.Equal(
            "integration-test-device",
            challenge.DeviceIdHash);

        Assert.Empty(
            await dbContext.RefreshTokens
                .ToListAsync());

        Assert.Empty(
            await dbContext.AuthSessions
                .ToListAsync());
    }
    [Fact]
    public async Task Login_WhenMaximumFailedAttemptsReached_ShouldPersistLockout()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string correctPassword =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            email,
            correctPassword,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        for (var attempt = 0;
             attempt < 5;
             attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new LoginRequest(
                    email,
                    "WrongPassword123!",
                    "integration-test-device",
                    "Integration Test Device"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var user =
            await dbContext.Users
                .SingleAsync(
                    x =>
                        x.NormalizedEmail ==
                        Email.Create(email)
                            .NormalizedValue);

        Assert.Equal(
            5,
            user.FailedLoginAttempts);

        Assert.NotNull(
            user.LastFailedLoginAtUtc);

        Assert.NotNull(
            user.LockoutEndUtc);

        Assert.True(
            user.LockoutEndUtc >
            DateTimeOffset.UtcNow);

        Assert.True(
            user.IsLockedOut(
                DateTimeOffset.UtcNow));
    }
    [Fact]
    public async Task Login_WhenAccountIsLocked_ShouldRejectCorrectPassword()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string correctPassword =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            email,
            correctPassword,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        for (var attempt = 0;
             attempt < 5;
             attempt++)
        {
            var failedResponse =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new LoginRequest(
                    email,
                    "WrongPassword123!",
                    "integration-test-device",
                    "Integration Test Device"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                failedResponse.StatusCode);
        }

        var lockedResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    correctPassword,
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            lockedResponse.StatusCode);

        var payload =
            await lockedResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "Login.AccountLocked",
            payload);
    }
    [Fact]
    public async Task Login_WhenLockoutHasExpired_ShouldAllowValidLogin()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string password =
            "SecretPassword123!";

        var user =
            await SeedUserAsync(
                application,
                email,
                password,
                confirmed:
                    true);

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var storedUser =
                await dbContext.Users
                    .SingleAsync(
                        x => x.Id == user.Id);

            storedUser.RecordFailedLoginAttempt(
                DateTimeOffset.UtcNow
                    .AddMinutes(-20));

            storedUser.RecordFailedLoginAttempt(
                DateTimeOffset.UtcNow
                    .AddMinutes(-19));

            storedUser.LockOutUntil(
                DateTimeOffset.UtcNow
                    .AddMinutes(-1));

            await dbContext.SaveChangesAsync();
        }

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        await using var verificationScope =
            application.Application.Services
                .CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var reloadedUser =
            await verificationDbContext.Users
                .SingleAsync(
                    x => x.Id == user.Id);

        Assert.Equal(
            0,
            reloadedUser.FailedLoginAttempts);

        Assert.Null(
            reloadedUser.LastFailedLoginAtUtc);

        Assert.Null(
            reloadedUser.LockoutEndUtc);
    }
    [Fact]
    public async Task Login_WhenRateLimitExceeded_ShouldReturn429()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var limitedResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            limitedResponse.StatusCode);
    }
    [Fact]
    public async Task Login_WithCustomRateLimit_ShouldUseConfiguredPermitLimit()
    {
        await using var application =
            await HttpTestApplication.CreateAsync(
                new LoginRateLimitOptions
                {
                    PermitLimit = 3,
                    Window =
                        TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });

        using var client =
            application.CreateClient();

        for (var attempt = 0;
             attempt < 3;
             attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var limitedResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            limitedResponse.StatusCode);
    }
    [Fact]
    public async Task Login_WhenEmailThrottleLimitReached_ShouldRejectRequest()
    {
        await using var application =
            await HttpTestApplication.CreateAsync(
                loginRateLimitOptions:
                    new LoginRateLimitOptions
                    {
                        PermitLimit = 100,
                        Window =
                            TimeSpan.FromMinutes(1)
                    },
                loginEmailThrottleOptions:
                    new LoginEmailThrottleOptions
                    {
                        MaxFailedAttempts = 3,
                        Window =
                            TimeSpan.FromMinutes(5)
                    });

        using var client =
            application.CreateClient();

        for (var attempt = 0;
             attempt < 3;
             attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var throttledResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "missing@example.com",
                    "Anything123!",
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            throttledResponse.StatusCode);
    }
    private static async Task<User> SeedUserAsync(
        HttpTestApplication application,
        string email,
        string password,
        bool confirmed)
    {
        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var passwordHasher =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHasher>();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    email),
                passwordHasher.Hash(
                    password),
                now.AddDays(-1));

        user.ClearDomainEvents();

        if (confirmed)
        {
            user.ConfirmEmail(
                now.AddHours(-1));

            user.ClearDomainEvents();
        }

        dbContext.Users.Add(
            user);

        await dbContext.SaveChangesAsync();

        return user;
    }
}














