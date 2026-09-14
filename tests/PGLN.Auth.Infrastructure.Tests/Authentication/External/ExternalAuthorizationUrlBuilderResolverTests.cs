using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Infrastructure.Authentication.External;
using PGLN.Auth.Infrastructure.Authentication.External.Google;

namespace PGLN.Auth.Infrastructure.Tests.Authentication.External;

public sealed class ExternalAuthorizationUrlBuilderResolverTests
{
    [Fact]
    public void GetBuilder_WhenProviderIsRegistered_ShouldReturnMatchingBuilder()
    {
        var googleOptions =
            new GoogleExternalAuthenticationOptions
            {
                ClientId =
                    "google-client-id",

                ClientSecret =
                    "google-client-secret"
            };

        IExternalAuthorizationUrlBuilder googleBuilder =
            new GoogleAuthorizationUrlBuilder(
                googleOptions);

        var resolver =
            new ExternalAuthorizationUrlBuilderResolver(
                new[]
                {
                    googleBuilder
                });

        var resolved =
            resolver.GetBuilder(
                ExternalLoginProvider.Google);

        Assert.Same(
            googleBuilder,
            resolved);
    }

    [Fact]
    public void GetBuilder_WhenProviderIsNotRegistered_ShouldThrow()
    {
        var resolver =
            new ExternalAuthorizationUrlBuilderResolver(
                Array.Empty<IExternalAuthorizationUrlBuilder>());

        var exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    resolver.GetBuilder(
                        ExternalLoginProvider.Google));

        Assert.Contains(
            "Google",
            exception.Message);
    }
}
