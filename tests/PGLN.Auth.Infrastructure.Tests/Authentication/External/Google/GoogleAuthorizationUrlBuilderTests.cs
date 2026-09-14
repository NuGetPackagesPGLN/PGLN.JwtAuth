using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Infrastructure.Authentication.External.Google;

namespace PGLN.Auth.Infrastructure.Tests.Authentication.External.Google;

public sealed class GoogleAuthorizationUrlBuilderTests
{
    [Fact]
    public void Build_ShouldCreateExpectedGoogleAuthorizationUrl()
    {
        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                AllowedRedirectUris =
                    new[]
                    {
                        "https://app.example.test/signin-google"
                    }
            };

        var builder =
            new GoogleAuthorizationUrlBuilder(
                options);

        var authorizationUrl =
            builder.Build(
                "https://app.example.test/signin-google",
                "protected-state",
                "pkce-code-challenge");

        var uri =
            new Uri(
                authorizationUrl);

        Assert.Equal(
            ExternalLoginProvider.Google,
            builder.Provider);

        Assert.Equal(
            "https",
            uri.Scheme);

        Assert.Equal(
            "accounts.google.com",
            uri.Host);

        Assert.Equal(
            "/o/oauth2/v2/auth",
            uri.AbsolutePath);

        var query =
            System.Web.HttpUtility.ParseQueryString(
                uri.Query);

        Assert.Equal(
            "google-client-id",
            query["client_id"]);

        Assert.Equal(
            "https://app.example.test/signin-google",
            query["redirect_uri"]);

        Assert.Equal(
            "code",
            query["response_type"]);

        Assert.Equal(
            "openid email profile",
            query["scope"]);

        Assert.Equal(
            "protected-state",
            query["state"]);

        Assert.Equal(
            "pkce-code-challenge",
            query["code_challenge"]);

        Assert.Equal(
            "S256",
            query["code_challenge_method"]);
    }

    [Fact]
    public void Build_WhenRedirectUriIsNotAllowed_ShouldThrow()
    {
        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret",

                AllowedRedirectUris =
                    new[]
                    {
                        "https://app.example.test/signin-google"
                    }
            };

        var builder =
            new GoogleAuthorizationUrlBuilder(
                options);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    builder.Build(
                        "https://attacker.example.test/callback",
                        "protected-state",
                        "pkce-code-challenge"));

        Assert.Equal(
            "The Google redirect URI is not allowed.",
            exception.Message);
    }}


