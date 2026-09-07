using PGLN.Auth.Application.Abstractions.Events;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Contracts.Common;
using PGLN.Auth.Contracts.EmailConfirmation;
using PGLN.Auth.Contracts.Registration;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class AuthEndpointHttpTests
{
    [Fact]
    public async Task Register_WithValidRequest_ShouldReturn201()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<RegisterResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            "user@example.com",
            body.Email);

        Assert.False(
            body.EmailConfirmed);

        Assert.NotEqual(
            Guid.Empty,
            body.UserId);
    }

    [Fact]
    public async Task Register_Response_ShouldNotExposeSensitiveFields()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var json =
            await response.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            "password",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "verificationToken",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "tokenHash",
            json,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "passwordHash",
            json,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var request =
            new RegisterRequest(
                "user@example.com",
                "SecretPassword123!");

        var first =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "USER@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        var error =
            await second.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "Registration.EmailAlreadyExists",
            error.Code);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400ValidationPayload()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "not-an-email",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var validation =
            await response.Content
                .ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.NotNull(validation);

        Assert.Equal(
            "Validation.Failed",
            validation.Code);

        Assert.Contains(
            validation.Errors.Keys,
            key =>
                string.Equals(
                    key,
                    "Email",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturn400()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "user@example.com",
                    "weak"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var validation =
            await response.Content
                .ReadFromJsonAsync<ValidationErrorResponse>();

        Assert.NotNull(validation);

        Assert.Contains(
            validation.Errors.Keys,
            key =>
                string.Equals(
                    key,
                    "Password",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ShouldReturn200()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var registrationResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var rawToken =
            await GetRawConfirmationTokenAsync(
                testApp);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email",
                new ConfirmEmailRequest(
                    rawToken));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ConfirmEmailResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            "user@example.com",
            body.Email);

        Assert.True(
            body.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ShouldReturn400()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email",
                new ConfirmEmailRequest(
                    "invalid-token"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>();

        Assert.NotNull(error);

        Assert.Equal(
            "EmailConfirmation.InvalidToken",
            error.Code);
    }

    [Fact]
    public async Task ConfirmEmail_WithUsedToken_ShouldReturn409()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "user@example.com",
                "SecretPassword123!"));

        var token =
            await GetRawConfirmationTokenAsync(
                testApp);

        var first =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email",
                new ConfirmEmailRequest(
                    token));

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email",
                new ConfirmEmailRequest(
                    token));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);
    }

        [Fact]
    public async Task ResendConfirmation_WithUnconfirmedUser_ShouldReturn202()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var registration =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Created,
            registration.StatusCode);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/resend-confirmation",
                new ResendEmailConfirmationRequest(
                    "user@example.com"));

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    ResendEmailConfirmationResponse>();

        Assert.NotNull(
            body);

        Assert.Equal(
            "If an unconfirmed account exists for that email address, a confirmation email has been sent.",
            body.Message);
    }

    [Fact]
    public async Task ResendConfirmation_WithUnknownEmail_ShouldReturn202()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/resend-confirmation",
                new ResendEmailConfirmationRequest(
                    "unknown@example.com"));

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    ResendEmailConfirmationResponse>();

        Assert.NotNull(
            body);

        Assert.Equal(
            "If an unconfirmed account exists for that email address, a confirmation email has been sent.",
            body.Message);
    }

    [Fact]
    public async Task ResendConfirmation_WithConfirmedUser_ShouldReturnSamePublicResponse()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var registration =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.Equal(
            HttpStatusCode.Created,
            registration.StatusCode);

        var token =
            await GetRawConfirmationTokenAsync(
                testApp);

        var confirmation =
            await client.PostAsJsonAsync(
                "/api/auth/confirm-email",
                new ConfirmEmailRequest(
                    token));

        Assert.Equal(
            HttpStatusCode.OK,
            confirmation.StatusCode);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/resend-confirmation",
                new ResendEmailConfirmationRequest(
                    "user@example.com"));

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<
                    ResendEmailConfirmationResponse>();

        Assert.NotNull(
            body);

        Assert.Equal(
            "If an unconfirmed account exists for that email address, a confirmation email has been sent.",
            body.Message);
    }

    [Fact]
    public async Task ResendConfirmation_KnownAndUnknownEmails_ShouldReturnIdenticalPublicResponses()
    {
        await using var knownApp =
            await HttpTestApplication.CreateAsync();

        using var knownClient =
            knownApp.CreateClient();

        await knownClient.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "user@example.com",
                "SecretPassword123!"));

        var knownResponse =
            await knownClient.PostAsJsonAsync(
                "/api/auth/resend-confirmation",
                new ResendEmailConfirmationRequest(
                    "user@example.com"));

        var knownJson =
            await knownResponse.Content
                .ReadAsStringAsync();

        await using var unknownApp =
            await HttpTestApplication.CreateAsync();

        using var unknownClient =
            unknownApp.CreateClient();

        var unknownResponse =
            await unknownClient.PostAsJsonAsync(
                "/api/auth/resend-confirmation",
                new ResendEmailConfirmationRequest(
                    "unknown@example.com"));

        var unknownJson =
            await unknownResponse.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.Accepted,
            knownResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Accepted,
            unknownResponse.StatusCode);

        Assert.Equal(
            knownJson,
            unknownJson);
    }

    [Fact]
    public async Task ResendConfirmation_WithInvalidEmail_ShouldReturn400()
    {
        await using var testApp =
            await HttpTestApplication.CreateAsync();

        using var client =
            testApp.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/resend-confirmation",
                new ResendEmailConfirmationRequest(
                    "not-an-email"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var validation =
            await response.Content
                .ReadFromJsonAsync<
                    ValidationErrorResponse>();

        Assert.NotNull(
            validation);

        Assert.Equal(
            "Validation.Failed",
            validation.Code);

        Assert.Contains(
            validation.Errors.Keys,
            key =>
                string.Equals(
                    key,
                    "Email",
                    StringComparison.OrdinalIgnoreCase));
    }

private static async Task<string> GetRawConfirmationTokenAsync(
        HttpTestApplication testApp)
    {
        using var scope =
            testApp.Application.Services
                .CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        var protector =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventPayloadProtector>();

        var outbox =
            await dbContext
                .OutboxMessages
                .OrderBy(
                    message =>
                        message.OccurredAtUtc)
                .FirstAsync();

        var payload =
            protector.Unprotect(
                outbox.Payload);

        using var document =
            System.Text.Json.JsonDocument.Parse(
                payload);

        return document
            .RootElement
            .GetProperty(
                "verificationToken")
            .GetString()
            ?? throw new InvalidOperationException(
                "Verification token was not present in the protected integration event.");
    }
}


