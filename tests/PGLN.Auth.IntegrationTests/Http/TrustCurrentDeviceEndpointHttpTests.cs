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

public sealed class TrustCurrentDeviceEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string Password =
        "SecretPassword123!";

    [Fact]
    public async Task TrustCurrentDevice_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task TrustCurrentDevice_WithValidAccessToken_ShouldReturn204()
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
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task TrustCurrentDevice_ShouldPersistTrustedDevice()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
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
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var trustedDevice =
            await dbContext
                .TrustedDevices
                .SingleAsync();

        Assert.Equal(
            user.Id,
            trustedDevice.UserId);

        Assert.Equal(
            "device-one",
            trustedDevice.DeviceIdHash);

        Assert.Equal(
            "Laptop",
            trustedDevice.DeviceName);

        Assert.True(
            trustedDevice.IsTrusted);

        Assert.False(
            trustedDevice.IsRevoked);
    }

    [Fact]
    public async Task TrustCurrentDevice_ShouldMarkCurrentSessionAsTrusted()
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
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

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

        Assert.True(
            session.IsTrustedDevice);
    }

    [Fact]
    public async Task TrustCurrentDevice_WhenCalledTwice_ShouldNotCreateDuplicate()
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

        var firstResponse =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        var secondResponse =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondResponse.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var trustedDevices =
            await dbContext
                .TrustedDevices
                .ToListAsync();

        Assert.Single(
            trustedDevices);
    }

}

