using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.ChangeEmail;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class ConfirmEmailChangeEndpointHttpTests
{
    private const string CurrentEmail =
        "current@example.com";

    private const string NewEmail =
        "new@example.com";

    private const string RawToken =
        "confirm-email-change-token";

    [Fact]
    public async Task ConfirmEmailChange_WithValidToken_ShouldReturn204()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedEmailChangeAsync(
            application,
            CurrentEmail,
            NewEmail,
            RawToken);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    RawToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailChange_WithValidToken_ShouldPersistNewEmail()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
            await SeedEmailChangeAsync(
                application,
                CurrentEmail,
                NewEmail,
                RawToken);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    RawToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var storedUser =
            await dbContext.Users
                .SingleAsync(
                    x => x.Id == user.Id);

        Assert.Equal(
            NewEmail,
            storedUser.Email.Value);

        Assert.Equal(
            Email.Create(NewEmail).NormalizedValue,
            storedUser.NormalizedEmail);

        Assert.True(
            storedUser.EmailConfirmed);

        Assert.NotNull(
            storedUser.EmailConfirmedAtUtc);
    }

    [Fact]
    public async Task ConfirmEmailChange_WithValidToken_ShouldMarkTokenAsUsed()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
            await SeedEmailChangeAsync(
                application,
                CurrentEmail,
                NewEmail,
                RawToken);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    RawToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var storedToken =
            await dbContext.EmailChangeTokens
                .SingleAsync(
                    x => x.UserId == user.Id);

        Assert.True(
            storedToken.IsUsed);

        Assert.NotNull(
            storedToken.UsedAtUtc);
    }

    [Fact]
    public async Task ConfirmEmailChange_WithInvalidToken_ShouldReturn400()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedEmailChangeAsync(
            application,
            CurrentEmail,
            NewEmail,
            RawToken);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    "invalid-token"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailChange_WithExpiredToken_ShouldReturn410()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedEmailChangeAsync(
            application,
            CurrentEmail,
            NewEmail,
            RawToken,
            expiresAtUtc:
                DateTimeOffset.UtcNow.AddMinutes(-1));

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    RawToken));

        Assert.Equal(
            HttpStatusCode.Gone,
            response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailChange_WithUsedToken_ShouldReturn409()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        var user =
            await SeedEmailChangeAsync(
                application,
                CurrentEmail,
                NewEmail,
                RawToken);

        await using (var scope =
            application.Application.Services
                .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var token =
                await dbContext.EmailChangeTokens
                    .SingleAsync(
                        x => x.UserId == user.Id);

            token.MarkAsUsed(
                DateTimeOffset.UtcNow);

            await dbContext.SaveChangesAsync();
        }

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    RawToken));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmailChange_WhenEmailAlreadyInUse_ShouldReturn409()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await SeedEmailChangeAsync(
            application,
            CurrentEmail,
            NewEmail,
            RawToken);

        await SeedUserAsync(
            application,
            NewEmail);

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    RawToken));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    private static async Task<User> SeedEmailChangeAsync(
        HttpTestApplication application,
        string currentEmail,
        string newEmail,
        string rawToken,
        DateTimeOffset? expiresAtUtc = null)
    {
        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var tokenHasher =
            scope.ServiceProvider
                .GetRequiredService<ITokenHasher>();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    currentEmail),
                "password-hash",
                now.AddDays(-1));

        user.ClearDomainEvents();

        user.ConfirmEmail(
            now.AddHours(-1));

        user.ClearDomainEvents();

        var expiration =
            expiresAtUtc ??
            now.AddMinutes(30);

        var createdAt =
            expiration <= now
                ? expiration.AddMinutes(-30)
                : now;

        var emailChangeToken =
            EmailChangeToken.Create(
                EmailChangeTokenId.New(),
                user.Id,
                Email.Create(
                    newEmail),
                tokenHasher.Hash(
                    rawToken),
                createdAt,
                expiration);

        dbContext.Users.Add(
            user);

        dbContext.EmailChangeTokens.Add(
            emailChangeToken);

        await dbContext.SaveChangesAsync();

        return user;
    }

    private static async Task<User> SeedUserAsync(
        HttpTestApplication application,
        string email)
    {
        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    email),
                "password-hash",
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
