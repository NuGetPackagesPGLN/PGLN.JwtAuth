using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.Infrastructure.Tests.DependencyInjection;

public sealed class ExternalAuthenticationDependencyInjectionTests
{
    [Fact]
    public void AddPGLNAuthInfrastructure_WhenGoogleIsConfigured_ShouldResolveGoogleProvider()
    {
        var settings =
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] = "https://issuer.test",
                ["PGLNAuth:Jwt:Audience"] = "pglnauth-tests",
                ["PGLNAuth:Jwt:SigningKey"] =
                    Convert.ToBase64String(
                        new byte[64]),
                ["PGLNAuth:StepUpSecurity:HmacSecret"] =
                    "this-is-a-test-step-up-secret-that-is-long-enough-123456789",

                ["PGLNAuth:ExternalAuthentication:Google:ClientId"] =
                    "google-client-id",

                ["PGLNAuth:ExternalAuthentication:Google:ClientSecret"] =
                    "google-client-secret"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    settings)
                .Build();

        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure(
            configuration);

        using var serviceProvider =
            services.BuildServiceProvider();

        var resolver =
            serviceProvider.GetRequiredService<
                IExternalIdentityProviderResolver>();

        var provider =
            resolver.GetProvider(
                ExternalLoginProvider.Google);

        Assert.NotNull(
            provider);

        Assert.Equal(
            ExternalLoginProvider.Google,
            provider.Provider);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_WhenGoogleIsNotConfigured_ShouldNotResolveGoogleProvider()
    {
        var settings =
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] = "https://issuer.test",
                ["PGLNAuth:Jwt:Audience"] = "pglnauth-tests",
                ["PGLNAuth:Jwt:SigningKey"] =
                    Convert.ToBase64String(
                        new byte[64]),
                ["PGLNAuth:StepUpSecurity:HmacSecret"] =
                    "this-is-a-test-step-up-secret-that-is-long-enough-123456789"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    settings)
                .Build();

        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure(
            configuration);

        using var serviceProvider =
            services.BuildServiceProvider();

        var resolver =
            serviceProvider.GetRequiredService<
                IExternalIdentityProviderResolver>();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => resolver.GetProvider(
                    ExternalLoginProvider.Google));

        Assert.Contains(
            "Google",
            exception.Message);
    }
}
