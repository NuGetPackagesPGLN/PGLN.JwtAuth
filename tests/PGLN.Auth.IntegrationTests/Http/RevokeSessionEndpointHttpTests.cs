using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.Sessions.GetSessions;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class RevokeSessionEndpointHttpTests
{
    private const string Password =
        "SecretPassword123!";

    [Fact]
    public async Task RevokeSession_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var sessionId =
            Guid.NewGuid();

        var response =
            await client.DeleteAsync(
                $"/api/auth/sessions/{sessionId}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeSession_WithOwnedSession_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            "user@example.com",
            Password);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                client,
                "user@example.com",
                Password,
                "device-one",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var sessionsResponse =
            await client.GetAsync(
                "/api/auth/sessions");

        Assert.Equal(
            HttpStatusCode.OK,
            sessionsResponse.StatusCode);

        var sessions =
            await sessionsResponse.Content
                .ReadFromJsonAsync<SessionItem[]>();

        Assert.NotNull(
            sessions);

        var session =
            Assert.Single(
                sessions);

        var response =
            await client.DeleteAsync(
                $"/api/auth/sessions/{session.Id.Value}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeSession_ShouldRevokeSessionAndRefreshTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            "user@example.com",
            Password);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                client,
                "user@example.com",
                Password,
                "device-one",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var sessionsResponse =
            await client.GetAsync(
                "/api/auth/sessions");

        var sessions =
            await sessionsResponse.Content
                .ReadFromJsonAsync<SessionItem[]>();

        Assert.NotNull(
            sessions);

        var session =
            Assert.Single(
                sessions);

        var response =
            await client.DeleteAsync(
                $"/api/auth/sessions/{session.Id.Value}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var storedSession =
            await dbContext
                .AuthSessions
                .SingleAsync(
                    item =>
                        item.Id == session.Id);

        Assert.True(
            storedSession.IsRevoked);

        Assert.Equal(
            "UserRevokedSession",
            storedSession.RevocationReason);

        var refreshTokens =
            await dbContext
                .RefreshTokens
                .Where(
                    token =>
                        token.SessionId == session.Id)
                .ToListAsync();

        Assert.NotEmpty(
            refreshTokens);

        Assert.All(
            refreshTokens,
            token =>
            {
                Assert.True(
                    token.IsRevoked);

                Assert.Equal(
                    "SessionRevoked",
                    token.RevocationReason);
            });
    }

    [Fact]
    public async Task RevokeSession_ForAnotherUsersSession_ShouldReturn404()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            "first@example.com",
            Password);

        await SeedConfirmedUserAsync(
            application,
            "second@example.com",
            Password);

        using var client =
            application.CreateClient();

        var firstUserLogin =
            await LoginAsync(
                client,
                "first@example.com",
                Password,
                "first-device",
                "First Device");

        var secondUserLogin =
            await LoginAsync(
                client,
                "second@example.com",
                Password,
                "second-device",
                "Second Device");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                secondUserLogin.AccessToken);

        var secondUserSessionsResponse =
            await client.GetAsync(
                "/api/auth/sessions");

        var secondUserSessions =
            await secondUserSessionsResponse.Content
                .ReadFromJsonAsync<SessionItem[]>();

        Assert.NotNull(
            secondUserSessions);

        var secondUserSession =
            Assert.Single(
                secondUserSessions);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                firstUserLogin.AccessToken);

        var response =
            await client.DeleteAsync(
                $"/api/auth/sessions/{secondUserSession.Id.Value}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeSession_WithUnknownSession_ShouldReturn404()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            "user@example.com",
            Password);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                client,
                "user@example.com",
                Password,
                "device-one",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.DeleteAsync(
                $"/api/auth/sessions/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(
        HttpClient client,
        string email,
        string password,
        string deviceIdHash,
        string? deviceName)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    deviceIdHash,
                    deviceName));

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
}
