using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Infrastructure.Authentication;

namespace PGLN.Auth.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterJwtAccessTokenGenerator()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure(
            CreateConfiguration());

        using var provider =
            services.BuildServiceProvider();

        var generator =
            provider.GetRequiredService<
                IAccessTokenGenerator>();

        Assert.IsType<
            JwtAccessTokenGenerator>(
            generator);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterSecureRefreshTokenGenerator()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure(
            CreateConfiguration());

        using var provider =
            services.BuildServiceProvider();

        var generator =
            provider.GetRequiredService<
                IRefreshTokenGenerator>();

        Assert.IsType<
            SecureRefreshTokenGenerator>(
            generator);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterJwtOptions()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure(
            CreateConfiguration());

        using var provider =
            services.BuildServiceProvider();

        var options =
            provider.GetRequiredService<
                JwtOptions>();

        Assert.Equal(
            "PGLN.Auth.Tests",
            options.Issuer);

        Assert.Equal(
            "PGLN.Auth.Tests.Client",
            options.Audience);

        Assert.Equal(
            TimeSpan.FromMinutes(15),
            options.AccessTokenLifetime);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_WithMissingJwtConfiguration_ShouldThrow()
    {
        var services =
            new ServiceCollection();

        var configuration =
            new ConfigurationBuilder()
                .Build();

        Assert.Throws<
            InvalidOperationException>(
            () =>
                services
                    .AddPGLNAuthInfrastructure(
                        configuration));
    }

    private static IConfiguration CreateConfiguration()
    {
        var values =
            new Dictionary<string, string?>
            {
                ["PGLNAuth:Jwt:Issuer"] =
                    "PGLN.Auth.Tests",

                ["PGLNAuth:Jwt:Audience"] =
                    "PGLN.Auth.Tests.Client",

                ["PGLNAuth:Jwt:SigningKey"] =
                    Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(
                            32)),

                ["PGLNAuth:Jwt:AccessTokenLifetime"] =
                    "00:15:00"
            };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                values)
            .Build();
    }
}
