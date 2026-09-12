using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.Sessions.GetSessions;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class GetSessionsEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string Password =
        "SecretPassword123!";

    [Fact]
    public async Task GetSessions_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.GetAsync(
                "/api/auth/sessions");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetSessions_WithValidAccessToken_ShouldReturnCurrentUsersSessions()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        var firstLogin =
            await LoginAsync(
                client,
                EmailAddress,
                Password,
                "device-one",
                "Laptop");

        var secondLogin =
            await LoginAsync(
                client,
                EmailAddress,
                Password,
                "device-two",
                "Phone");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                secondLogin.AccessToken);

        var response =
            await client.GetAsync(
                "/api/auth/sessions");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var sessions =
            await response.Content
                .ReadFromJsonAsync<SessionItem[]>();

        Assert.NotNull(
            sessions);

        Assert.Equal(
            2,
            sessions.Length);

        Assert.Contains(
            sessions,
            session =>
                session.DeviceIdHash == "device-one" &&
                session.DeviceName == "Laptop");

        Assert.Contains(
            sessions,
            session =>
                session.DeviceIdHash == "device-two" &&
                session.DeviceName == "Phone");
    }

    [Fact]
    public async Task GetSessions_ShouldNotReturnSessionsBelongingToAnotherUser()
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
                "first-user-device",
                "First User Device");

        await LoginAsync(
            client,
            "second@example.com",
            Password,
            "second-user-device",
            "Second User Device");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                firstUserLogin.AccessToken);

        var response =
            await client.GetAsync(
                "/api/auth/sessions");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var sessions =
            await response.Content
                .ReadFromJsonAsync<SessionItem[]>();

        Assert.NotNull(
            sessions);

        Assert.Single(
            sessions);

        Assert.Equal(
            "first-user-device",
            sessions[0].DeviceIdHash);

        Assert.Equal(
            "First User Device",
            sessions[0].DeviceName);
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
