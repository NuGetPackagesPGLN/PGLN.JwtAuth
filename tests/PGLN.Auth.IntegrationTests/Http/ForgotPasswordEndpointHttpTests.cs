using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class ForgotPasswordEndpointHttpTests
{
    [Fact]
    public async Task ForgotPassword_WithKnownEmail_ShouldReturn204AndCreateResetToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "forgot-password@example.com";

        UserId userId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        email),
                    passwordHasher.Hash(
                        "Password123!"),
                    DateTimeOffset.UtcNow.AddDays(-1));

            user.ClearDomainEvents();

            userId =
                user.Id;

            dbContext.Users.Add(
                user);

            await dbContext.SaveChangesAsync();
        }

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new
                {
                    Email =
                        email
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var resetToken =
                await dbContext.PasswordResetTokens
                    .SingleAsync();

            Assert.Equal(
                userId,
                resetToken.UserId);

            Assert.False(
                resetToken.IsUsed);

            Assert.True(
                resetToken.ExpiresAtUtc >
                resetToken.CreatedAtUtc);
        }
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_ShouldReturn204AndNotCreateResetToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new
                {
                    Email =
                        "missing@example.com"
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        Assert.Empty(
            await dbContext.PasswordResetTokens
                .ToListAsync());
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_ShouldReturn400AndNotCreateResetToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new
                {
                    Email =
                        "not-an-email"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        Assert.Empty(
            await dbContext.PasswordResetTokens
                .ToListAsync());
    }

    [Fact]
    public async Task ForgotPassword_WithRepeatedRequest_ShouldInvalidatePreviousToken()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string email =
            "repeated-forgot-password@example.com";

        UserId userId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var passwordHasher =
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>();

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        email),
                    passwordHasher.Hash(
                        "Password123!"),
                    DateTimeOffset.UtcNow.AddDays(-1));

            user.ClearDomainEvents();

            userId =
                user.Id;

            dbContext.Users.Add(
                user);

            await dbContext.SaveChangesAsync();
        }

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new
                {
                    Email =
                        email
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new
                {
                    Email =
                        email
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondResponse.StatusCode);

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var tokens =
                await dbContext.PasswordResetTokens
                    .Where(
                        token =>
                            token.UserId == userId)
                    .OrderBy(
                        token =>
                            token.CreatedAtUtc)
                    .ToListAsync();

            Assert.Equal(
                2,
                tokens.Count);

            Assert.Single(
                tokens,
                token =>
                    token.IsUsed);

            Assert.Single(
                tokens,
                token =>
                    !token.IsUsed);

            var activeToken =
                tokens.Single(
                    token =>
                        !token.IsUsed);

            Assert.True(
                activeToken.CanBeUsed(
                    DateTimeOffset.UtcNow));
        }
    }
}

