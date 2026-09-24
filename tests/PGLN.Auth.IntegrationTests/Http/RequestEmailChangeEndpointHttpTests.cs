using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Contracts.Authentication;
using PGLN.Auth.Domain.Users;using PGLN.Auth.Application.Abstractions.Events;using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Contracts.ChangeEmail;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class RequestEmailChangeEndpointHttpTests
{
    private const string EmailAddress =
        "user@example.com";

    private const string CurrentPassword =
        "CurrentPassword123!";

    private const string NewEmailAddress =
        "new-email@example.com";

    [Fact]
    public async Task RequestEmailChange_WithoutAccessToken_ShouldReturn401()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    NewEmailAddress,
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RequestEmailChange_WithValidRequest_ShouldCreatePendingTokenWithoutChangingEmail()
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
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    NewEmailAddress,
                    CurrentPassword));

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
            EmailAddress,
            storedUser.Email.Value);

        var emailChangeToken =
            await dbContext.EmailChangeTokens
                .SingleAsync(
                    x => x.UserId == user.Id);

        Assert.Equal(
            NewEmailAddress,
            emailChangeToken.NewEmail);

        Assert.Equal(
            NewEmailAddress.ToUpperInvariant(),
            emailChangeToken.NormalizedNewEmail);

        Assert.False(
            emailChangeToken.IsUsed);

        Assert.Null(
            emailChangeToken.UsedAtUtc);

        Assert.False(
            string.IsNullOrWhiteSpace(
                emailChangeToken.TokenHash));

        Assert.True(
            emailChangeToken.ExpiresAtUtc >
            emailChangeToken.CreatedAtUtc);
    }

    [Fact]
    public async Task RequestEmailChange_WithWrongCurrentPassword_ShouldReturn400()
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
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    NewEmailAddress,
                    "WrongPassword123!"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    [Fact]
    public async Task RequestEmailChange_WithCurrentEmail_ShouldReturn400()
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
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    EmailAddress,
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    [Fact]
    public async Task RequestEmailChange_WithEmailAlreadyInUse_ShouldReturn409()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            EmailAddress,
            CurrentPassword);

        const string existingEmail =
            "existing-user@example.com";

        await HttpAuthenticationHelper.SeedConfirmedUserAsync(
            application,
            existingEmail,
            "ExistingUserPassword123!");

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
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    existingEmail,
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }
    [Fact]
    public async Task RequestEmailChange_WithInvalidEmail_ShouldReturn400()
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
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    "not-an-email",
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    [Fact]
    public async Task RequestEmailChange_WhenRequestedAgain_ShouldRevokePreviousToken()
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

        const string firstNewEmail =
            "first-new-email@example.com";

        const string secondNewEmail =
            "second-new-email@example.com";

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    firstNewEmail,
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    secondNewEmail,
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondResponse.StatusCode);

        await using var scope =
            application.Application.Services
                .CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var tokens =
            (await dbContext.EmailChangeTokens
                .Where(
                    x => x.UserId == user.Id)
                .ToListAsync())
                .OrderBy(
                    x => x.CreatedAtUtc)
                .ToList();

        Assert.Equal(
            2,
            tokens.Count);

        var firstToken =
            tokens[0];

        var secondToken =
            tokens[1];

        Assert.Equal(
            firstNewEmail,
            firstToken.NewEmail);

        Assert.True(
            firstToken.IsUsed);

        Assert.NotNull(
            firstToken.UsedAtUtc);

        Assert.Equal(
            secondNewEmail,
            secondToken.NewEmail);

        Assert.False(
            secondToken.IsUsed);

        Assert.Null(
            secondToken.UsedAtUtc);

        Assert.NotEqual(
            firstToken.TokenHash,
            secondToken.TokenHash);
    }
    [Fact]
    public async Task RequestEmailChange_ThenConfirm_ShouldChangeEmail()
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

        var requestResponse =
            await client.PostAsJsonAsync(
                "/api/auth/change-email",
                new RequestEmailChangeRequest(
                    NewEmailAddress,
                    CurrentPassword));

        Assert.Equal(
            HttpStatusCode.NoContent,
            requestResponse.StatusCode);

        string rawToken;

        await using (var scope =
            application.Application.Services
                .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var protector =
                scope.ServiceProvider
                    .GetRequiredService<IIntegrationEventPayloadProtector>();

            var outboxMessages =
                await dbContext.OutboxMessages
                    .ToListAsync();

            var outbox =
                outboxMessages
                    .OrderByDescending(
                        x => x.OccurredAtUtc)
                    .First();

            var payload =
                protector.Unprotect(
                    outbox.Payload);

            using var document =
                System.Text.Json.JsonDocument.Parse(
                    payload);

            rawToken =
                document.RootElement
                    .GetProperty(
                        "verificationToken")
                    .GetString()
                ?? throw new InvalidOperationException(
                    "Verification token was not present in the email-change integration event.");
        }

        client.DefaultRequestHeaders.Authorization =
            null;

        var confirmResponse =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email-change",
                new ConfirmEmailChangeRequest(
                    rawToken));

        Assert.Equal(
            HttpStatusCode.NoContent,
            confirmResponse.StatusCode);

        await using var verificationScope =
            application.Application.Services
                .CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var storedUser =
            await verificationDbContext.Users
                .SingleAsync(
                    x => x.Id == user.Id);

        var storedToken =
            await verificationDbContext.EmailChangeTokens
                .SingleAsync(
                    x => x.UserId == user.Id);

        Assert.Equal(
            NewEmailAddress,
            storedUser.Email.Value);

        Assert.Equal(
            Email.Create(NewEmailAddress).NormalizedValue,
            storedUser.NormalizedEmail);

        Assert.True(
            storedUser.EmailConfirmed);

        Assert.True(
            storedToken.IsUsed);

        Assert.NotNull(
            storedToken.UsedAtUtc);
        var storedSession =
            await verificationDbContext.AuthSessions
                .SingleAsync(
                    x => x.UserId == user.Id);

        Assert.True(
            storedSession.IsRevoked);

        Assert.NotNull(
            storedSession.RevokedAtUtc);

        Assert.Equal(
            "Email address changed.",
            storedSession.RevocationReason);
        var verificationProtector =
            verificationScope.ServiceProvider
                .GetRequiredService<IIntegrationEventPayloadProtector>();

        var verificationOutboxMessages =
            await verificationDbContext.OutboxMessages
                .ToListAsync();

        var emailChangedOutboxMessage =
            verificationOutboxMessages
                .Single(
                    message =>
                        message.Type.Contains(
                            nameof(EmailChangedNotificationRequested),
                            StringComparison.Ordinal));

        var emailChangedPayloadJson =
            verificationProtector.Unprotect(
                emailChangedOutboxMessage.Payload);

        using var emailChangedDocument =
            System.Text.Json.JsonDocument.Parse(
                emailChangedPayloadJson);

        var emailChangedPayload =
            emailChangedDocument.RootElement;

        Assert.Equal(
            EmailAddress,
            emailChangedPayload
                .GetProperty("oldEmail")
                .GetString());

        Assert.Equal(
            NewEmailAddress,
            emailChangedPayload
                .GetProperty("newEmail")
                .GetString());
        // The access token belongs to the session that existed
        // before the email address changed. It must now be invalid.

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var protectedResponse =
            await client.GetAsync(
                "/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            protectedResponse.StatusCode);

        // The refresh token belongs to the same old session.
        // It must also be unusable after the email change.

        client.DefaultRequestHeaders.Authorization =
            null;

        var refreshResponse =
            await client.PostAsJsonAsync(
                "/api/auth/refresh",
                new RefreshTokenRequest(
                    login.RefreshToken));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
        // The old email address must no longer authenticate the user.

        var oldEmailLoginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    EmailAddress,
                    CurrentPassword,
                    "old-email-login-device",
                    "Old Email Login Device"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldEmailLoginResponse.StatusCode);

        // The new email address now represents the same account.
        // The helper handles either direct authentication or step-up.

        var newEmailLogin =
            await HttpAuthenticationHelper.LoginExistingUserAsync(
                client,
                NewEmailAddress,
                CurrentPassword,
                deviceIdHash:
                    "new-email-login-device",
                deviceName:
                    "New Email Login Device");

        Assert.Equal(
            user.Id.Value,
            newEmailLogin.UserId);

        Assert.Equal(
            NewEmailAddress,
            newEmailLogin.Email);

        Assert.False(
            string.IsNullOrWhiteSpace(
                newEmailLogin.AccessToken));

        Assert.False(
            string.IsNullOrWhiteSpace(
                newEmailLogin.RefreshToken));
    }}
















