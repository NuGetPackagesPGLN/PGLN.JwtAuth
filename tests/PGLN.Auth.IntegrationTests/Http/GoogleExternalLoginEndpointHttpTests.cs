using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace PGLN.Auth.IntegrationTests.Http;

public sealed class GoogleExternalLoginEndpointHttpTests
{
    [Fact]
    public async Task StartGoogleLogin_WithAllowedRedirectUri_ShouldRedirectToGoogle()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string redirectUri =
            "https://app.example.test/signin-google";

        var requestUri =
            $"/api/auth/external/google/start" +
            $"?redirectUri={Uri.EscapeDataString(redirectUri)}";

        var response =
            await client.GetAsync(
                requestUri);

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.NotNull(
            response.Headers.Location);

        var location =
            response.Headers.Location!;

        Assert.Equal(
            "https",
            location.Scheme);

        Assert.Equal(
            "accounts.google.com",
            location.Host);

        Assert.Equal(
            "/o/oauth2/v2/auth",
            location.AbsolutePath);

        var query =
            QueryHelpers.ParseQuery(
                location.Query);

        Assert.Equal(
            "integration-test-google-client-id",
            query["client_id"].ToString());

        Assert.Equal(
            redirectUri,
            query["redirect_uri"].ToString());

        Assert.Equal(
            "code",
            query["response_type"].ToString());

        Assert.Equal(
            "openid email profile",
            query["scope"].ToString());

        Assert.Equal(
            "S256",
            query["code_challenge_method"].ToString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                query["code_challenge"].ToString()));

        Assert.False(
            string.IsNullOrWhiteSpace(
                query["state"].ToString()));
    }

    [Fact]
    public async Task StartGoogleLogin_WithUnapprovedRedirectUri_ShouldRejectRequest()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string redirectUri =
            "https://attacker.example/callback";

        var requestUri =
            $"/api/auth/external/google/start" +
            $"?redirectUri={Uri.EscapeDataString(redirectUri)}";

        var response =
            await client.GetAsync(
                requestUri);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var payload =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "ExternalAuthentication.RedirectUriNotAllowed",
            payload);
    }
    [Fact]
    public async Task CompleteGoogleLogin_WithValidCallback_ShouldCreateUserAndReturnTokens()
    {
        await using var application =
            await HttpTestApplication.CreateAsync();

        using var client =
            application.CreateClient();

        const string redirectUri =
            "https://app.example.test/signin-google";

        var startRequestUri =
            $"/api/auth/external/google/start" +
            $"?redirectUri={Uri.EscapeDataString(redirectUri)}";

        var startResponse =
            await client.GetAsync(
                startRequestUri);

        Assert.Equal(
            HttpStatusCode.Redirect,
            startResponse.StatusCode);

        Assert.NotNull(
            startResponse.Headers.Location);

        var authorizationUri =
            startResponse.Headers.Location!;

        var authorizationQuery =
            Microsoft.AspNetCore.WebUtilities.QueryHelpers
                .ParseQuery(
                    authorizationUri.Query);

        Assert.True(
            authorizationQuery.TryGetValue(
                "state",
                out var stateValues));

        var state =
            stateValues.ToString();

        Assert.False(
            string.IsNullOrWhiteSpace(
                state));

        var callbackRequestUri =
            $"/api/auth/external/google/callback" +
            $"?code={Uri.EscapeDataString("integration-test-code")}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&redirectUri={Uri.EscapeDataString(redirectUri)}" +
            $"&deviceIdHash={Uri.EscapeDataString("integration-test-device")}" +
            $"&deviceName={Uri.EscapeDataString("Integration Test Device")}";

        var callbackResponse =
            await client.GetAsync(
                callbackRequestUri);

        Assert.Equal(
            HttpStatusCode.OK,
            callbackResponse.StatusCode);

        var payload =
            await callbackResponse.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "google-user@example.com",
            payload);

        Assert.Contains(
            "\"isNewUser\":true",
            payload);

        Assert.Contains(
            "\"accessToken\"",
            payload);

        Assert.Contains(
            "\"refreshToken\"",
            payload);
    }}



