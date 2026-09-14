using PGLN.Auth.Infrastructure.Authentication.External.Google;

namespace PGLN.Auth.Infrastructure.Tests.Authentication.External.Google;

public sealed class GoogleExternalAuthenticationOptionsTests
{
    [Fact]
    public void Validate_WhenConfigurationIsValid_ShouldNotThrow()
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

        var exception =
            Record.Exception(
                options.Validate);

        Assert.Null(
            exception);
    }

    [Fact]
    public void Validate_WhenNoRedirectUrisAreConfigured_ShouldThrow()
    {
        var options =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret"
            };

        var exception =
            Assert.Throws<InvalidOperationException>(
                options.Validate);

        Assert.Equal(
            "At least one Google redirect URI must be configured.",
            exception.Message);
    }

    [Fact]
    public void Validate_WhenProductionRedirectUriUsesHttp_ShouldThrow()
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
                        "http://app.example.test/signin-google"
                    }
            };

        Assert.Throws<InvalidOperationException>(
            options.Validate);
    }

    [Fact]
    public void Validate_WhenLocalhostRedirectUriUsesHttp_ShouldAllowIt()
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
                        "http://localhost:5000/signin-google"
                    }
            };

        var exception =
            Record.Exception(
                options.Validate);

        Assert.Null(
            exception);
    }
}
