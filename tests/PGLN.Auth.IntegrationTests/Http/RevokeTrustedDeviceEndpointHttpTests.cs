using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Features.TrustedDevices.GetTrustedDevices;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class RevokeTrustedDeviceEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string Password =
        "SecretPassword123!";

    [Fact]
    public async Task RevokeTrustedDevice_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.DeleteAsync(
                $"/api/auth/trusted-devices/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeTrustedDevice_WhenDeviceExists_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                Password,
                "device-one",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        await TrustCurrentDeviceAsync(
            client);

        var trustedDevice =
            await GetSingleTrustedDeviceAsync(
                client);

        var response =
            await client.DeleteAsync(
                $"/api/auth/trusted-devices/{trustedDevice.Id.Value}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeTrustedDevice_ShouldPersistRevocationAndRemoveTrustFromActiveSession()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                Password,
                "device-one",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        await TrustCurrentDeviceAsync(
            client);

        var trustedDevice =
            await GetSingleTrustedDeviceAsync(
                client);

        var response =
            await client.DeleteAsync(
                $"/api/auth/trusted-devices/{trustedDevice.Id.Value}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using (var scope =
            application.Application.Services
                .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var persistedDevice =
                await dbContext.TrustedDevices
                    .SingleAsync();

            Assert.True(
                persistedDevice.IsRevoked);

            Assert.False(
                persistedDevice.IsTrusted);

            Assert.NotNull(
                persistedDevice.RevokedAtUtc);

            Assert.Equal(
                "UserRevokedTrust",
                persistedDevice.RevocationReason);

            var session =
                await dbContext.AuthSessions
                    .SingleAsync(
                        session =>
                            session.DeviceIdHash ==
                            "device-one");

            Assert.Equal(
                PGLN.Auth.Domain.Sessions.DeviceTrustStatus.Revoked,
                session.DeviceTrustStatus);

            Assert.False(
                session.IsTrustedDevice);

            Assert.False(
                session.IsRevoked);

            Assert.True(
                session.IsActive);

            Assert.Null(
                session.RevokedAtUtc);
        }

        var authenticatedResponse =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.OK,
            authenticatedResponse.StatusCode);
    }
    [Fact]
    public async Task RevokeTrustedDevice_WhenDeviceDoesNotExist_ShouldReturn404()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            Password);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                Password,
                "device-one",
                "Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.DeleteAsync(
                $"/api/auth/trusted-devices/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokeTrustedDevice_WhenOwnedByAnotherUser_ShouldReturn404()
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

        var firstLogin =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                "first@example.com",
                Password,
                "first-device",
                "First Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                firstLogin.AccessToken);

        await TrustCurrentDeviceAsync(
            client);

        var firstDevice =
            await GetSingleTrustedDeviceAsync(
                client);

        var secondLogin =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                "second@example.com",
                Password,
                "second-device",
                "Second Laptop");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                secondLogin.AccessToken);

        var response =
            await client.DeleteAsync(
                $"/api/auth/trusted-devices/{firstDevice.Id.Value}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static async Task TrustCurrentDeviceAsync(
        HttpClient client)
    {
        var response =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private static async Task<TrustedDeviceResponse> GetSingleTrustedDeviceAsync(
        HttpClient client)
    {
        var response =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var devices =
            await response.Content
                .ReadFromJsonAsync<TrustedDeviceResponse[]>();

        Assert.NotNull(
            devices);

        return Assert.Single(
            devices);
    }

}


