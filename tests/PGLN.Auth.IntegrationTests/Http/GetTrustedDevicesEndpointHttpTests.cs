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

public sealed class GetTrustedDevicesEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string Password =
        "SecretPassword123!";

    [Fact]
    public async Task GetTrustedDevices_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTrustedDevices_WithTrustedDevice_ShouldReturnDevice()
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

        var trustResponse =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            trustResponse.StatusCode);

        var response =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            "deviceIdHash",
            json,
            StringComparison.OrdinalIgnoreCase);

        var devices =
            await response.Content
                .ReadFromJsonAsync<TrustedDeviceResponse[]>();

        Assert.NotNull(
            devices);

        var device =
            Assert.Single(
                devices);

        Assert.Equal(
            "Laptop",
            device.DeviceName);

        Assert.True(
            device.IsTrusted);

        Assert.Null(
            device.RevokedAtUtc);
    }

    [Fact]
    public async Task GetTrustedDevices_ShouldOnlyReturnCurrentUsersDevices()
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

        var firstTrustResponse =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstTrustResponse.StatusCode);

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

        var secondTrustResponse =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondTrustResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                firstLogin.AccessToken);

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

        var device =
            Assert.Single(
                devices);

        Assert.Equal(
            "First Laptop",
            device.DeviceName);
    }

    [Fact]
    public async Task GetTrustedDevices_AfterInitialLogin_ShouldReturnInitialDevice()
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

        var device =
            Assert.Single(
                devices);

        Assert.Equal(
            "Laptop",
            device.DeviceName);

        Assert.True(
            device.IsTrusted);

        Assert.Null(
            device.RevokedAtUtc);
    }

}
