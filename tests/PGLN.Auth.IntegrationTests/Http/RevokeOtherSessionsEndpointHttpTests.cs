using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class RevokeOtherSessionsEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string Password =
        "SecretPassword123!";

    [Fact]
    public async Task RevokeOtherSessions_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.DeleteAsync(
                "/api/auth/sessions/others");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeOtherSessions_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        await LoginAsync(
            client,
            EmailAddress,
            Password,
            "device-one",
            "Laptop");

        var currentLogin =
            await LoginAsync(
                client,
                EmailAddress,
                Password,
                "device-two",
                "Phone");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                currentLogin.AccessToken);

        var response =
            await client.DeleteAsync(
                "/api/auth/sessions/others");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeOtherSessions_ShouldRevokeOtherSessionAndItsRefreshTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        await LoginAsync(
            client,
            EmailAddress,
            Password,
            "device-one",
            "Laptop");

        var currentLogin =
            await LoginAsync(
                client,
                EmailAddress,
                Password,
                "device-two",
                "Phone");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                currentLogin.AccessToken);

        var response =
            await client.DeleteAsync(
                "/api/auth/sessions/others");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var sessions =
            await dbContext
                .AuthSessions
                .OrderBy(
                    session =>
                        session.DeviceIdHash)
                .ToListAsync();

        Assert.Equal(
            2,
            sessions.Count);

        var otherSession =
            sessions.Single(
                session =>
                    session.DeviceIdHash ==
                    "device-one");

        var currentSession =
            sessions.Single(
                session =>
                    session.DeviceIdHash ==
                    "device-two");

        Assert.True(
            otherSession.IsRevoked);

        Assert.Equal(
            "UserRevokedOtherSessions",
            otherSession.RevocationReason);

        Assert.False(
            currentSession.IsRevoked);

        Assert.Null(
            currentSession.RevocationReason);

        var otherRefreshTokens =
            await dbContext
                .RefreshTokens
                .Where(
                    token =>
                        token.SessionId ==
                        otherSession.Id)
                .ToListAsync();

        Assert.NotEmpty(
            otherRefreshTokens);

        Assert.All(
            otherRefreshTokens,
            token =>
            {
                Assert.True(
                    token.IsRevoked);

                Assert.Equal(
                    "SessionRevoked",
                    token.RevocationReason);
            });

        var currentRefreshTokens =
            await dbContext
                .RefreshTokens
                .Where(
                    token =>
                        token.SessionId ==
                        currentSession.Id)
                .ToListAsync();

        Assert.NotEmpty(
            currentRefreshTokens);

        Assert.All(
            currentRefreshTokens,
            token =>
                Assert.False(
                    token.IsRevoked));
    }

    [Fact]
    public async Task RevokeOtherSessions_WhenNoOtherSessionsExist_ShouldKeepCurrentSessionActive()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
                client,
                EmailAddress,
                Password,
                "current-device",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.DeleteAsync(
                "/api/auth/sessions/others");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var session =
            await dbContext
                .AuthSessions
                .SingleAsync();

        Assert.False(
            session.IsRevoked);

        var refreshTokens =
            await dbContext
                .RefreshTokens
                .Where(
                    token =>
                        token.SessionId ==
                        session.Id)
                .ToListAsync();

        Assert.NotEmpty(
            refreshTokens);

        Assert.All(
            refreshTokens,
            token =>
                Assert.False(
                    token.IsRevoked));
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
