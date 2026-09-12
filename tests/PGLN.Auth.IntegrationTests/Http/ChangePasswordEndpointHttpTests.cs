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

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
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

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
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

        await SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
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
            await SeedConfirmedUserAsync(
                application,
                EmailAddress,
                CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
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
                storedUser.PasswordHash));

        Assert.False(
            passwordHasher.Verify(
                CurrentPassword,
                storedUser.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_ShouldRevokeActiveRefreshTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
            await SeedConfirmedUserAsync(
                application,
                EmailAddress,
                CurrentPassword);

        using var client =
            application.CreateClient();

        var login =
            await LoginAsync(
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

    private static async Task<LoginResponse> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    password,
                    "change-password-test-device",
                    "Integration Test Device"));

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

