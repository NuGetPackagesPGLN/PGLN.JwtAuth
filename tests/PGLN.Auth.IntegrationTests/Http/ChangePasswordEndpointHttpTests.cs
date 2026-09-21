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

public sealed class ChangePasswordEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string CurrentPassword =
        "CurrentPassword123!";

    private const string NewPassword =
        "NewPassword456!";

    [Fact]
    public async Task ChangePassword_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    CurrentPassword,
                    NewPassword));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidAccessToken_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                CurrentPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    CurrentPassword,
                    NewPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                CurrentPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    "WrongPassword123!",
                    NewPassword));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                CurrentPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    CurrentPassword,
                    "weak"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ShouldUpdateStoredPasswordHash()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
            await HttpAuthenticationHelper.SeedConfirmedUserAsync(
                application,
                EmailAddress,
                CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                CurrentPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    CurrentPassword,
                    NewPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var passwordHasher =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHasher>();

        var storedUser =
            await dbContext.Users
                .SingleAsync(
                    x => x.Id == user.Id);

        Assert.True(
            passwordHasher.Verify(
                NewPassword,
                storedUser.PasswordHash!));

        Assert.False(
            passwordHasher.Verify(
                CurrentPassword,
                storedUser.PasswordHash!));
    }

    [Fact]
    public async Task ChangePassword_ShouldInvalidateExistingAccessAndRefreshTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                CurrentPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var changePasswordResponse =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    CurrentPassword,
                    NewPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            changePasswordResponse.StatusCode);

        var protectedResponse =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            protectedResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            null;

        var refreshResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken!));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
    }
    [Fact]
    public async Task ChangePassword_ShouldRevokeActiveRefreshTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
            await HttpAuthenticationHelper.SeedConfirmedUserAsync(
                application,
                EmailAddress,
                CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                EmailAddress,
                CurrentPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-password",
                new ChangePasswordRequest(
                    CurrentPassword,
                    NewPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var refreshTokens =
            await dbContext.RefreshTokens
                .Where(
                    x => x.UserId == user.Id)
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
                    "PasswordChanged",
                    token.RevocationReason);
            });
    }

}




