using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using System.Net;
using System.Text;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Infrastructure.Authentication.External.Google;
using PGLN.Auth.Infrastructure.Tests.TestDoubles;

namespace PGLN.Auth.Infrastructure.Tests.Authentication.External.Google;

public sealed class GoogleExternalIdentityProviderTests
{
    [Fact]
    public async Task GetIdentityAsync_WhenGoogleReturnsValidResponses_ShouldMapExternalIdentity()
    {
        var handler =
            new FakeHttpMessageHandler();

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "access_token": "google-access-token",
                          "expires_in": 3600,
                          "token_type": "Bearer",
                          "scope": "openid email profile",
                          "id_token": "google-id-token"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "sub": "google-subject-123",
                          "email": "user@example.com",
                          "email_verified": true,
                          "name": "Example User",
                          "given_name": "Example",
                          "family_name": "User"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        using var httpClient =
            new HttpClient(
                handler);

        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                TokenEndpoint =
                    "https://accounts.example.test/token",

                UserInfoEndpoint =
                    "https://accounts.example.test/userinfo",

                AllowedRedirectUris =
                [
                    "https://app.example.test/signin-google"
                ]
            };

        var provider =
            new GoogleExternalIdentityProvider(
                httpClient,
                options);

        var identity =
            await provider.GetIdentityAsync(
                "authorization-code",
                "https://app.example.test/signin-google",
                "test-code-verifier");

        Assert.Equal(
            ExternalLoginProvider.Google,
            identity.Provider);

        Assert.Equal(
            "google-subject-123",
            identity.ProviderSubject);

        Assert.Equal(
            "user@example.com",
            identity.Email);

        Assert.True(
            identity.EmailVerified);

        Assert.Equal(
            "Example User",
            identity.DisplayName);

        Assert.Equal(
            2,
            handler.Requests.Count);

        Assert.Equal(
            HttpMethod.Post,
            handler.Requests[0].Method);

        Assert.Equal(
            HttpMethod.Get,
            handler.Requests[1].Method);

        Assert.Equal(
            "Bearer",
            handler.Requests[1].AuthorizationScheme);

        Assert.Equal(
            "google-access-token",
            handler.Requests[1].AuthorizationParameter);
    }

    [Fact]
    public async Task GetIdentityAsync_WhenTokenExchangeFails_ShouldThrow()
    {
        var handler =
            new FakeHttpMessageHandler();

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.BadRequest)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "error": "invalid_grant"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        using var httpClient =
            new HttpClient(
                handler);

        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                TokenEndpoint =
                    "https://accounts.example.test/token",

                UserInfoEndpoint =
                    "https://accounts.example.test/userinfo",

                AllowedRedirectUris =
                [
                    "https://app.example.test/signin-google"
                ]
            };

        var provider =
            new GoogleExternalIdentityProvider(
                httpClient,
                options);

        var exception =
            await Assert.ThrowsAsync<ExternalIdentityProviderException>(
                () =>
                    provider.GetIdentityAsync(
                        "invalid-authorization-code",
                        "https://app.example.test/signin-google",
                        "test-code-verifier"));

        Assert.Equal(
            "Google authorization-code exchange failed.",
            exception.Message);

        Assert.Single(
            handler.Requests);

        Assert.Equal(
            HttpMethod.Post,
            handler.Requests[0].Method);
    }

    [Fact]
    public async Task GetIdentityAsync_WhenUserInfoRequestFails_ShouldThrow()
    {
        var handler =
            new FakeHttpMessageHandler();

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "access_token": "google-access-token",
                          "expires_in": 3600,
                          "token_type": "Bearer"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.Unauthorized));

        using var httpClient =
            new HttpClient(
                handler);

        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                TokenEndpoint =
                    "https://accounts.example.test/token",

                UserInfoEndpoint =
                    "https://accounts.example.test/userinfo",

                AllowedRedirectUris =
                [
                    "https://app.example.test/signin-google"
                ]
            };

        var provider =
            new GoogleExternalIdentityProvider(
                httpClient,
                options);

        var exception =
            await Assert.ThrowsAsync<ExternalIdentityProviderException>(
                () =>
                    provider.GetIdentityAsync(
                        "authorization-code",
                        "https://app.example.test/signin-google",
                        "test-code-verifier"));

        Assert.Equal(
            "Google user-info request failed.",
            exception.Message);

        Assert.Equal(
            2,
            handler.Requests.Count);

        Assert.Equal(
            HttpMethod.Post,
            handler.Requests[0].Method);

        Assert.Equal(
            HttpMethod.Get,
            handler.Requests[1].Method);
    }

    [Fact]
    public async Task GetIdentityAsync_WhenGoogleSubjectIsMissing_ShouldThrow()
    {
        var handler =
            new FakeHttpMessageHandler();

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "access_token": "google-access-token",
                          "expires_in": 3600,
                          "token_type": "Bearer"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "email": "user@example.com",
                          "email_verified": true,
                          "name": "Example User"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        using var httpClient =
            new HttpClient(
                handler);

        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                TokenEndpoint =
                    "https://accounts.example.test/token",

                UserInfoEndpoint =
                    "https://accounts.example.test/userinfo",

                AllowedRedirectUris =
                [
                    "https://app.example.test/signin-google"
                ]
            };

        var provider =
            new GoogleExternalIdentityProvider(
                httpClient,
                options);

        var exception =
            await Assert.ThrowsAsync<ExternalIdentityProviderException>(
                () =>
                    provider.GetIdentityAsync(
                        "authorization-code",
                        "https://app.example.test/signin-google",
                        "test-code-verifier"));

        Assert.Equal(
            "Google did not return a valid subject identifier.",
            exception.Message);

        Assert.Equal(
            2,
            handler.Requests.Count);
    }

    [Fact]
    public async Task GetIdentityAsync_ShouldSendExpectedAuthorizationCodeExchangeRequest()
    {
        var handler =
            new FakeHttpMessageHandler();

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "access_token": "google-access-token",
                          "expires_in": 3600,
                          "token_type": "Bearer"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        handler.Enqueue(
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content =
                    new StringContent(
                        """
                        {
                          "sub": "google-subject-123",
                          "email": "user@example.com",
                          "email_verified": true,
                          "name": "Example User"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
            });

        using var httpClient =
            new HttpClient(
                handler);

        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                TokenEndpoint =
                    "https://accounts.example.test/token",

                UserInfoEndpoint =
                    "https://accounts.example.test/userinfo",

                AllowedRedirectUris =
                [
                    "https://app.example.test/signin-google"
                ]
            };

        var provider =
            new GoogleExternalIdentityProvider(
                httpClient,
                options);

        await provider.GetIdentityAsync(
            "authorization-code-123",
            "https://app.example.test/signin-google",
            "test-code-verifier");

        var tokenRequest =
            handler.Requests[0];

        Assert.Equal(
            HttpMethod.Post,
            tokenRequest.Method);

        Assert.Equal(
            options.TokenEndpoint,
            tokenRequest.RequestUri?.ToString());

        Assert.NotNull(
            tokenRequest.Body);

        var body =
            tokenRequest.Body!;

        Assert.Contains(
            "code=authorization-code-123",
            body);

        Assert.Contains(
            "client_id=google-client-id",
            body);

        Assert.Contains(
            "client_secret=google-client-secret",
            body);

        Assert.Contains(
            "redirect_uri=https%3A%2F%2Fapp.example.test%2Fsignin-google",
            body);

        Assert.Contains(
            "grant_type=authorization_code",
            body);

        Assert.Contains(
            "code_verifier=test-code-verifier",
            body);
    }
}












