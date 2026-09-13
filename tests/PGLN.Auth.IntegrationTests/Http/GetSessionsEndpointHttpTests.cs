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

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        var firstLogin =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                Password,
                "device-one",
                "Laptop");

        var secondLogin =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
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

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            "first@example.com",
            Password);

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            "second@example.com",
            Password);

        using var client =
            application.CreateClient();

        var firstUserLogin =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                "first@example.com",
                Password,
                "first-user-device",
                "First User Device");

        await HttpAuthenticationHelper.LoginExistingUserAsync(
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

}

