using PGLN.Auth.IntegrationTests.TestHelpers;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class RefreshTokenEndpointHttpTests
{
    [Fact]
    public async Task Refresh_WithValidToken_ShouldReturnNewTokenPair()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginAsync(
                application,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(
            body);

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.AccessToken));

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.RefreshToken!));

        Assert.NotEqual(
            login.RefreshToken!,
            body.RefreshToken!);

        Assert.True(
            body.AccessTokenExpiresAtUtc >
            DateTimeOffset.UtcNow);

        Assert.True(
            body.RefreshTokenExpiresAtUtc >
            body.AccessTokenExpiresAtUtc);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ShouldRotatePersistedToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginAsync(
                application,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(
            body);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var tokens =
            await dbContext.RefreshTokens
                .ToListAsync();

        Assert.Equal(
            2,
            tokens.Count);

        var oldToken =
            tokens.Single(
                token =>
                    token.IsRevoked);

        var replacement =
            tokens.Single(
                token =>
                    !token.IsRevoked);

        Assert.Equal(
            "Rotated",
            oldToken.RevocationReason);

        Assert.Equal(
            replacement.Id,
            oldToken.ReplacedByTokenId);

        Assert.NotEqual(
            body.RefreshToken!,
            replacement.TokenHash);
    }

    [Fact]
    public async Task Refresh_ReusingRotatedToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginAsync(
                application,
                client);

        var first =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            second.StatusCode);

        var body =
            await second.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.RevokedToken",
            body);
    }

    [Fact]
    public async Task Refresh_ReusingRotatedToken_ShouldRevokeTokenFamily()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginAsync(
                application,
                client);

        // Token A -> Token B
        var firstRefresh =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.OK,
            firstRefresh.StatusCode);

        var firstRefreshBody =
            await firstRefresh.Content
                .ReadFromJsonAsync<RefreshTokenResponse>();

        Assert.NotNull(
            firstRefreshBody);

        var replacementRawToken =
            firstRefreshBody.RefreshToken;

        // Replay Token A.
        var replay =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            replay.StatusCode);

        // Verify Token B was revoked because the family
        // is now considered compromised.
        await using (
            var scope =
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

        // Token B must now also be unusable.
        var compromisedReplacement =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    replacementRawToken));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            compromisedReplacement.StatusCode);

        var body =
            await compromisedReplacement.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.RevokedToken",
            body);
    }
    [Fact]
    public async Task Refresh_WithUnknownToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    "unknown-refresh-token"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.InvalidToken",
            body);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var rawRefreshToken =
            "expired-raw-refresh-token";

        await SeedExpiredTokenAsync(
            application,
            rawRefreshToken);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    rawRefreshToken));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.ExpiredToken",
            body);
    }

    private static async Task SeedExpiredTokenAsync(
        HttpTestApplication application,
        string rawRefreshToken)
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

        var tokenHasher =
            scope.ServiceProvider
                .GetRequiredService<ITokenHasher>();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "expired@example.com"),
                passwordHasher.Hash(
                    "SecretPassword123!"),
                now.AddDays(-40));

        user.ClearDomainEvents();

        user.ConfirmEmail(
            now.AddDays(-39));

        user.ClearDomainEvents();

        var session =
            AuthSessionTestFactory.Create(
                user.Id,
                now.AddDays(-31));

        dbContext.AuthSessions.Add(
            session);

        var token =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id,
                session.Id,
                tokenHasher.Hash(
                    rawRefreshToken),
                now.AddDays(-31),
                now.AddSeconds(-1));

        dbContext.Users.Add(
            user);

        dbContext.RefreshTokens.Add(
            token);

        await dbContext.SaveChangesAsync();
    }
}







