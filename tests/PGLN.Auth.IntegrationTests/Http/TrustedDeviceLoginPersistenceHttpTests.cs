using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class TrustedDeviceLoginPersistenceHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string Password =
        "SecretPassword123!";

    private const string DeviceIdHash =
        "device-hash-001";

    private const string DeviceName =
        "Chrome on Windows";

    [Fact]
    public async Task Login_AfterTrustedDeviceWasLoggedOut_ShouldTrustNewSession()
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
                DeviceIdHash,
                DeviceName);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                firstLogin.AccessToken);

        var trustResponse =
            await client.PostAsync(
                "/api/auth/trusted-devices/current",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            trustResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            null;

        var logoutResponse =
            await client.PostAsJsonAsync(
                "/api/auth/logout",
                new LogoutRequest(
                    firstLogin.RefreshToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                firstLogin.AccessToken);

        var oldAccessTokenResponse =
            await client.GetAsync(
                "/api/auth/trusted-devices");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldAccessTokenResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            null;

        var secondLogin =
            await LoginAsync(
                client,
                EmailAddress,
                Password,
                DeviceIdHash,
                DeviceName);

        Assert.NotNull(
            secondLogin.AccessToken);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var sessions =
            await dbContext.AuthSessions
                .Where(
                    session =>
                        session.DeviceIdHash ==
                        DeviceIdHash)
                .OrderBy(
                    session =>
                        session.CreatedAtUtc)
                .ToArrayAsync();

        Assert.Equal(
            2,
            sessions.Length);

        var revokedSession =
            Assert.Single(
                sessions,
                session =>
                    session.IsRevoked);

        Assert.Equal(
            "Logout",
            revokedSession.RevocationReason);

        Assert.NotNull(
            revokedSession.RevokedAtUtc);

        var activeSession =
            Assert.Single(
                sessions,
                session =>
                    !session.IsRevoked);

        Assert.True(
            activeSession.IsTrustedDevice);

        Assert.Equal(
            DeviceTrustStatus.Trusted,
            activeSession.DeviceTrustStatus);

        var trustedDevices =
            await dbContext.TrustedDevices
                .Where(
                    device =>
                        device.DeviceIdHash ==
                        DeviceIdHash)
                .ToArrayAsync();

        var trustedDevice =
            Assert.Single(
                trustedDevices);

        Assert.True(
            trustedDevice.IsTrusted);

        Assert.False(
            trustedDevice.IsRevoked);
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
