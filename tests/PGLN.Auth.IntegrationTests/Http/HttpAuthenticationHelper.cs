using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

internal static class HttpAuthenticationHelper
{
    private const string DefaultEmail =
        "user@example.com";

    private const string DefaultPassword =
        "SecretPassword123!";

    private const string DefaultDeviceIdHash =
        "integration-test-device";

    private const string DefaultDeviceName =
        "Integration Test Device";

    private const string DefaultStepUpCode =
        "123456";

    public static async Task<AuthenticatedLogin> LoginAsync(
        HttpTestApplication application,
        HttpClient client,
        string email = DefaultEmail,
        string password = DefaultPassword,
        string deviceIdHash = DefaultDeviceIdHash,
        string deviceName = DefaultDeviceName,
        bool rememberDevice = false)
    {
        await SeedConfirmedUserAsync(
            application,
            email,
            password);

        return await LoginExistingUserAsync(
            client,
            email,
            password,
            deviceIdHash,
            deviceName,
            rememberDevice);
    }
    public static async Task<AuthenticatedLogin> LoginExistingUserAsync(
        HttpClient client,
        string email,
        string password = DefaultPassword,
        string deviceIdHash = DefaultDeviceIdHash,
        string deviceName = DefaultDeviceName,
        bool rememberDevice = false)
    {
        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    deviceIdHash,
                    deviceName));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            login);

        if (
            login.Status ==
            "AuthenticationComplete")
        {
            Assert.False(
                string.IsNullOrWhiteSpace(
                    login.AccessToken));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    login.RefreshToken));

            return new AuthenticatedLogin(
                login.UserId,
                login.Email,
                login.AccessToken!,
                login.AccessTokenExpiresAtUtc!.Value,
                login.RefreshToken!,
                login.RefreshTokenExpiresAtUtc!.Value);
        }

        Assert.Equal(
            "StepUpRequired",
            login.Status);

        Assert.NotNull(
            login.StepUpChallengeId);

        var verifyResponse =
            await client.PostAsJsonAsync(
                "/api/auth/verify-step-up",
                new VerifyStepUpRequest(
                    login.StepUpChallengeId.Value,
                    DefaultStepUpCode,
                    rememberDevice));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var verified =
            await verifyResponse.Content
                .ReadFromJsonAsync<VerifyStepUpResponse>();

        Assert.NotNull(
            verified);

        Assert.False(
            string.IsNullOrWhiteSpace(
                verified.AccessToken));

        Assert.False(
            string.IsNullOrWhiteSpace(
                verified.RefreshToken));

        return new AuthenticatedLogin(
            verified.UserId,
            verified.Email,
            verified.AccessToken,
            verified.AccessTokenExpiresAtUtc,
            verified.RefreshToken,
            verified.RefreshTokenExpiresAtUtc);
    }
    public static async Task<User> SeedConfirmedUserAsync(
        HttpTestApplication application,
        string email = DefaultEmail,
        string password = DefaultPassword)
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

internal sealed record AuthenticatedLogin(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);


