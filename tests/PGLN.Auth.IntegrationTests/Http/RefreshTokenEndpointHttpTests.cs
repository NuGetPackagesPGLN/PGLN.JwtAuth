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
            await LoginAsync(
                application,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken));

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
                body.RefreshToken));

        Assert.NotEqual(
            login.RefreshToken,
            body.RefreshToken);

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
            await LoginAsync(
                application,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken));

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
            body.RefreshToken,
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
            await LoginAsync(
                application,
                client);

        var first =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken));

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

    private static async Task<LoginResponse> LoginAsync(
        HttpTestApplication application,
        HttpClient client)
    {
        const string email =
            "user@example.com";

        const string password =
            "SecretPassword123!";

        await SeedConfirmedUserAsync(
            application,
            email,
            password);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            body);

        return body;
    }

    private static async Task<User> SeedConfirmedUserAsync(
        HttpTestApplication application,
        string email,
        string password)
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

        user.ConfirmEmail(
            now.AddHours(-1));

        user.ClearDomainEvents();

        dbContext.Users.Add(
            user);

        await dbContext.SaveChangesAsync();

        return user;
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

        var token =
            RefreshToken.Create(
                RefreshTokenId.New(),
                user.Id,
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

