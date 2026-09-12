using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class LogoutAllEndpointHttpTests
{
    [Fact]
    public async Task LogoutAll_WithValidRefreshToken_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                application,
                client,
                "logout-all-user@example.com");

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeAllActiveSessionsForUser()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var firstLogin =
            await LoginAsync(
                application,
                client,
                "multi-session@example.com");

        var secondLogin =
            await LoginExistingUserAsync(
                client,
                "multi-session@example.com");

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    firstLogin.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

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

        Assert.All(
            tokens,
            token =>
            {
                Assert.True(
                    token.IsRevoked);

                Assert.Equal(
                    "LogoutAll",
                    token.RevocationReason);

                Assert.NotNull(
                    token.RevokedAtUtc);
            });
    }

    [Fact]
    public async Task LogoutAll_ShouldMakeAllUserRefreshTokensUnusable()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var firstLogin =
            await LoginAsync(
                application,
                client,
                "invalidate-all@example.com");

        var secondLogin =
            await LoginExistingUserAsync(
                client,
                "invalidate-all@example.com");

        var logoutAll =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    firstLogin.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutAll.StatusCode);

        var firstRefresh =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    firstLogin.RefreshToken));

        var secondRefresh =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    secondLogin.RefreshToken));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            firstRefresh.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            secondRefresh.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_ShouldNotRevokeAnotherUsersSession()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var targetLogin =
            await LoginAsync(
                application,
                client,
                "target-user@example.com");

        var unrelatedLogin =
            await LoginAsync(
                application,
                client,
                "unrelated-user@example.com");

        var logoutAll =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    targetLogin.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutAll.StatusCode);

        var unrelatedRefresh =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    unrelatedLogin.RefreshToken));

        Assert.Equal(
            HttpStatusCode.OK,
            unrelatedRefresh.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithUnknownToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    "unknown-refresh-token"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithRevokedToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                application,
                client,
                "revoked-anchor@example.com");

        var logout =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logout.StatusCode);

        var logoutAll =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            logoutAll.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithEmptyRefreshToken_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/logout-all",
                new LogoutAllRequest(
                    string.Empty));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(
        HttpTestApplication application,
        HttpClient client,
        string email)
    {
        const string password =
            "SecretPassword123!";

        await SeedConfirmedUserAsync(
            application,
            email,
            password);

        return await LoginExistingUserAsync(
            client,
            email);
    }

    private static async Task<LoginResponse> LoginExistingUserAsync(
        HttpClient client,
        string email)
    {
        const string password =
            "SecretPassword123!";

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
}

