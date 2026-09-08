using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class LoginEndpointHttpTests
{
    [Fact]
    public async Task Login_WithValidConfirmedUser_ShouldReturn200AndTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string password =
            "SecretPassword123!";

        await SeedUserAsync(
            application,
            "user@example.com",
            password,
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    password));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            body);

        Assert.Equal(
            "user@example.com",
            body.Email);

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.AccessToken));

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.RefreshToken));

        Assert.True(
            body.AccessTokenExpiresAtUtc >
            DateTimeOffset.UtcNow);

        Assert.True(
            body.RefreshTokenExpiresAtUtc >
            body.AccessTokenExpiresAtUtc);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedUserAsync(
            application,
            "user@example.com",
            "SecretPassword123!",
            confirmed:
                true);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    "WrongPassword123!"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnSame401AsWrongPassword()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "missing@example.com",
                    "Anything123!"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "Login.InvalidCredentials",
            payload);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ShouldReturn403()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedUserAsync(
            application,
            "user@example.com",
            "SecretPassword123!",
            confirmed:
                false);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldPersistSuccessfulAttemptAndRefreshToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        const string password =
            "SecretPassword123!";

        var user =
            await SeedUserAsync(
                application,
                "user@example.com",
                password,
                confirmed:
                    true);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    "user@example.com",
                    password));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(
            body);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var attempt =
            await dbContext.LoginAttempts
                .SingleAsync();

        Assert.True(
            attempt.Succeeded);

        Assert.Equal(
            user.Id,
            attempt.UserId);

        var refreshToken =
            await dbContext.RefreshTokens
                .SingleAsync();

        Assert.Equal(
            user.Id,
            refreshToken.UserId);

        Assert.NotEqual(
            body.RefreshToken,
            refreshToken.TokenHash);
    }

    private static async Task<User> SeedUserAsync(
        HttpTestApplication application,
        string email,
        string password,
        bool confirmed)
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

        if (confirmed)
        {
            user.ConfirmEmail(
                now.AddHours(-1));

            user.ClearDomainEvents();
        }

        dbContext.Users.Add(
            user);

        await dbContext.SaveChangesAsync();

        return user;
    }
}
