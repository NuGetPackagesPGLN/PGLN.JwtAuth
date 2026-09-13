using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class VerifyStepUpEndpointHttpTests
{
    [Fact]
    public async Task VerifyStepUp_WithValidCode_ShouldIssueTokensAndCreateSession()
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

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    password,
                    "integration-test-device",
                    "Integration Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            loginBody);

        Assert.Equal(
            "StepUpRequired",
            loginBody.Status);

        Assert.NotNull(
            loginBody.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    loginBody.StepUpChallengeId.Value,
                    "123456",
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var verifyBody =
            await verifyResponse.Content
                .ReadFromJsonAsync<VerifyStepUpResponse>();

        Assert.NotNull(
            verifyBody);

        Assert.False(
            string.IsNullOrWhiteSpace(
                verifyBody.AccessToken));

        Assert.False(
            string.IsNullOrWhiteSpace(
                verifyBody.RefreshToken));

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var challenge =
            await dbContext.StepUpChallenges
                .SingleAsync();

        Assert.NotNull(
            challenge.VerifiedAtUtc);

        var session =
            await dbContext.AuthSessions
                .SingleAsync();

        Assert.Equal(
            "integration-test-device",
            session.DeviceIdHash);

        Assert.False(
            session.IsRevoked);

        var refreshToken =
            await dbContext.RefreshTokens
                .SingleAsync();

        Assert.Equal(
            session.Id,
            refreshToken.SessionId);
    }


    [Fact]
    public async Task VerifyStepUp_WithInvalidCode_ShouldReturnUnauthorizedAndNotCreateSessionOrTokens()
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

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    password,
                    "integration-test-device",
                    "Integration Test Device"));

        var loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            loginBody);

        Assert.NotNull(
            loginBody.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    loginBody.StepUpChallengeId.Value,
                    "654321",
                    false));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            verifyResponse.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var challenge =
            await dbContext.StepUpChallenges
                .SingleAsync();

        Assert.Null(
            challenge.VerifiedAtUtc);

        Assert.Equal(
            1,
            challenge.FailedAttempts);

        Assert.Empty(
            await dbContext.AuthSessions
                .ToListAsync());

        Assert.Empty(
            await dbContext.RefreshTokens
                .ToListAsync());
    }

    [Fact]
    public async Task VerifyStepUp_WithoutRememberDevice_ShouldRequireStepUpAgainAfterLogout()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string password =
            "SecretPassword123!";

        const string deviceIdHash =
            "untrusted-device-001";

        const string deviceName =
            "Untrusted Test Device";

        await SeedUserAsync(
            application,
            email,
            password,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var firstLoginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    deviceIdHash,
                    deviceName));

        Assert.Equal(
            HttpStatusCode.OK,
            firstLoginResponse.StatusCode);

        var firstLogin =
            await firstLoginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            firstLogin);

        Assert.Equal(
            "StepUpRequired",
            firstLogin.Status);

        Assert.NotNull(
            firstLogin.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    firstLogin.StepUpChallengeId.Value,
                    "123456",
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var verified =
            await verifyResponse.Content
                .ReadFromJsonAsync<VerifyStepUpResponse>();

        Assert.NotNull(
            verified);

        await using (var scope =
            application.Application.Services
                .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var session =
                await dbContext.AuthSessions
                    .SingleAsync();

            Assert.False(
                session.IsTrustedDevice);

            Assert.Empty(
                await dbContext.TrustedDevices
                    .ToListAsync());
        }

        var logoutResponse =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    verified.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        var secondLoginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    deviceIdHash,
                    deviceName));

        Assert.Equal(
            HttpStatusCode.OK,
            secondLoginResponse.StatusCode);

        var secondLogin =
            await secondLoginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            secondLogin);

        Assert.Equal(
            "StepUpRequired",
            secondLogin.Status);

        Assert.NotNull(
            secondLogin.StepUpChallengeId);

        Assert.Null(
            secondLogin.AccessToken);

        Assert.Null(
            secondLogin.RefreshToken);
    }

    [Fact]
    public async Task VerifyStepUp_WithValidCode_ShouldIssueUsableAccessToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string password =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            email,
            password,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    "access-token-device",
                    "Access Token Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            login);

        Assert.Equal(
            "StepUpRequired",
            login.Status);

        Assert.NotNull(
            login.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    login.StepUpChallengeId.Value,
                    "123456",
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var verified =
            await verifyResponse.Content
                .ReadFromJsonAsync<VerifyStepUpResponse>();

        Assert.NotNull(
            verified);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                verified.AccessToken);

        var protectedResponse =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.OK,
            protectedResponse.StatusCode);
    }

    [Fact]
    public async Task VerifyStepUp_RefreshToken_ShouldRotateAndIssueUsableAccessToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string password =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            email,
            password,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    "refresh-token-device",
                    "Refresh Token Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            login);

        Assert.Equal(
            "StepUpRequired",
            login.Status);

        Assert.NotNull(
            login.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    login.StepUpChallengeId.Value,
                    "123456",
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var verified =
            await verifyResponse.Content
                .ReadFromJsonAsync<VerifyStepUpResponse>();

        Assert.NotNull(
            verified);

        var originalRefreshToken =
            verified.RefreshToken;

        var refreshResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    originalRefreshToken));

        Assert.Equal(
            HttpStatusCode.OK,
            refreshResponse.StatusCode);

        var refreshed =
            await refreshResponse.Content
                .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(
            refreshed);

        Assert.False(
            string.IsNullOrWhiteSpace(
                refreshed.AccessToken));

        Assert.False(
            string.IsNullOrWhiteSpace(
                refreshed.RefreshToken));

        Assert.NotEqual(
            originalRefreshToken,
            refreshed.RefreshToken);

        Assert.Equal(
            verified.UserId,
            refreshed.UserId);

        Assert.Equal(
            verified.Email,
            refreshed.Email);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                refreshed.AccessToken);

        var protectedResponse =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.OK,
            protectedResponse.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var refreshTokens =
            (await dbContext.RefreshTokens
                .ToArrayAsync())
            .OrderBy(
                token =>
                    token.CreatedAtUtc)
            .ToArray();

        Assert.Equal(
            2,
            refreshTokens.Length);

        var oldToken =
            refreshTokens[0];

        var newToken =
            refreshTokens[1];

        Assert.NotNull(
            oldToken.RevokedAtUtc);

        Assert.NotNull(
            oldToken.ReplacedByTokenId);

        Assert.Equal(
            newToken.Id,
            oldToken.ReplacedByTokenId);

        Assert.Equal(
            oldToken.SessionId,
            newToken.SessionId);

        Assert.Equal(
            oldToken.FamilyId,
            newToken.FamilyId);

        Assert.Null(
            newToken.RevokedAtUtc);
    }

    [Fact]
    public async Task VerifyStepUp_ReplayingRotatedRefreshToken_ShouldRevokeTokenFamily()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string email =
            "user@example.com";

        const string password =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            email,
            password,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    "replay-test-device",
                    "Replay Test Device"));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            login);

        Assert.Equal(
            "StepUpRequired",
            login.Status);

        Assert.NotNull(
            login.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    login.StepUpChallengeId.Value,
                    "123456",
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var verified =
            await verifyResponse.Content
                .ReadFromJsonAsync<VerifyStepUpResponse>();

        Assert.NotNull(
            verified);

        var tokenA =
            verified.RefreshToken;

        // A -> B
        var firstRefreshResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    tokenA));

        Assert.Equal(
            HttpStatusCode.OK,
            firstRefreshResponse.StatusCode);

        var firstRefresh =
            await firstRefreshResponse.Content
                .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(
            firstRefresh);

        var tokenB =
            firstRefresh.RefreshToken;

        Assert.NotEqual(
            tokenA,
            tokenB);

        // Replay A.
        var replayResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    tokenA));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            replayResponse.StatusCode);

        var replayBody =
            await replayResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.RevokedToken",
            replayBody);

        // The replay should compromise the whole family,
        // including replacement token B.
        await using (var scope =
            application.Application.Services
                .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var tokens =
                await dbContext.RefreshTokens
                    .AsNoTracking()
                    .ToListAsync();

            Assert.Equal(
                2,
                tokens.Count);

            var original =
                tokens.Single(
                    token =>
                        token.ReplacedByTokenId.HasValue);

            var replacement =
                tokens.Single(
                    token =>
                        token.Id ==
                        original.ReplacedByTokenId!.Value);

            Assert.Equal(
                original.FamilyId,
                replacement.FamilyId);

            Assert.Equal(
                original.SessionId,
                replacement.SessionId);

            Assert.Equal(
                "Rotated",
                original.RevocationReason);

            Assert.True(
                replacement.IsRevoked);

            Assert.NotNull(
                replacement.RevokedAtUtc);

            Assert.Equal(
                "RefreshTokenReuseDetected",
                replacement.RevocationReason);
        }

        // A refresh-token replay represents session compromise.
        // The access token issued with Token B should therefore
        // also become unusable.
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                firstRefresh.AccessToken);

        var compromisedAccessTokenResponse =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            compromisedAccessTokenResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            null;

        // Token B must also be unusable.
        var compromisedReplacementResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    tokenB));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            compromisedReplacementResponse.StatusCode);

        var compromisedBody =
            await compromisedReplacementResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.RevokedToken",
            compromisedBody);
    }
    private static async Task SeedUserAsync(
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
                .GetRequiredService<
                    PGLN.Auth.Application.Abstractions.Authentication.IPasswordHasher>();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            PGLN.Auth.Domain.Users.User.Register(
                PGLN.Auth.Domain.Users.UserId.New(),
                PGLN.Auth.Domain.Users.Email.Create(
                    email),
                passwordHasher.Hash(
                    password),
                now);

        if (confirmed)
        {
            user.ConfirmEmail(
                now);
        }

        dbContext.Users.Add(
            user);

        await dbContext.SaveChangesAsync();
    }
}







