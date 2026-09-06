using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Infrastructure.Authentication;
using PGLN.Auth.Infrastructure.Passwords;
using PGLN.Auth.Infrastructure.Time;

namespace PGLN.Auth.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterClock()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure();

        using var provider =
            services.BuildServiceProvider();

        var clock =
            provider.GetRequiredService<IClock>();

        Assert.IsType<SystemClock>(
            clock);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterPasswordHasher()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure();

        using var provider =
            services.BuildServiceProvider();

        var passwordHasher =
            provider.GetRequiredService<IPasswordHasher>();

        Assert.IsType<PasswordHasher>(
            passwordHasher);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterVerificationTokenGenerator()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure();

        using var provider =
            services.BuildServiceProvider();

        var generator =
            provider.GetRequiredService<IVerificationTokenGenerator>();

        Assert.IsType<
            SecureVerificationTokenGenerator>(
            generator);
    }

    [Fact]
    public void AddPGLNAuthInfrastructure_ShouldRegisterTokenHasher()
    {
        var services =
            new ServiceCollection();

        services.AddPGLNAuthInfrastructure();

        using var provider =
            services.BuildServiceProvider();

        var hasher =
            provider.GetRequiredService<ITokenHasher>();

        Assert.IsType<Sha256TokenHasher>(
            hasher);
    }
}
