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

public sealed class LogoutEndpointHttpTests
{
    [Fact]
    public async Task Logout_WithActiveRefreshToken_ShouldReturn204()
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
                "/api/auth/logout",
                new LogoutRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokePersistedRefreshToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                application,
                client);

        var logoutResponse =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var refreshToken =
            await dbContext.RefreshTokens
                .SingleAsync();

        Assert.True(
            refreshToken.IsRevoked);

        Assert.NotNull(
            refreshToken.RevokedAtUtc);

        Assert.Equal(
            "Logout",
            refreshToken.RevocationReason);

        Assert.Null(
            refreshToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task Logout_ThenRefresh_WithSameToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                application,
                client);

        var logoutResponse =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        var refreshResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);

        var body =
            await refreshResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "TokenRefresh.RevokedToken",
            body);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    "unknown-refresh-token"));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task Logout_Twice_WithSameToken_ShouldReturn204BothTimes()
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
                "/api/auth/logout",
                new LogoutRequest(
                    login.RefreshToken));

        var second =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            first.StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            second.StatusCode);
    }

    [Fact]
    public async Task Logout_WithExpiredToken_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string rawRefreshToken =
            "expired-logout-refresh-token";

        await SeedExpiredTokenAsync(
            application,
            rawRefreshToken);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    rawRefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithEmptyRefreshToken_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    string.Empty));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(
        HttpTestApplication application,
        HttpClient client)
    {
        const string email =
            "logout-user@example.com";

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

        return body;
    }

    private static async Task SeedConfirmedUserAsync(
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
                    "expired-logout@example.com"),
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



