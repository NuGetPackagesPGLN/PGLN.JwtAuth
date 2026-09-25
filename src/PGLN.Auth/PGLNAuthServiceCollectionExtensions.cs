using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application;
using PGLN.Auth.AspNetCore.Authentication;
using PGLN.Auth.EntityFrameworkCore;
using PGLN.Auth.EntityFrameworkCore.PostgreSql;
using PGLN.Auth.Infrastructure;

namespace PGLN.Auth;

public static class PGLNAuthServiceCollectionExtensions
{
    public static IServiceCollection AddPGLNAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDatabase,
        Action<PGLNAuthOptions>? configureAuth = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureDatabase);

        var authOptions =
            new PGLNAuthOptions();

        configureAuth?.Invoke(
            authOptions);

        services.AddPGLNAuthApplication(
            passwordPolicy:
                authOptions.PasswordPolicy,
            emailVerificationOptions:
                authOptions.EmailVerification,
            emailDeliveryOptions:
                authOptions.EmailDelivery,
            refreshTokenOptions:
                authOptions.RefreshTokens,
            accountLockoutOptions:
                authOptions.AccountLockout,
            loginEmailThrottleOptions:
                authOptions.LoginEmailThrottle,
            stepUpChallengeOptions:
                authOptions.StepUp,
            passwordResetOptions:
                authOptions.PasswordReset);

        services.AddPGLNAuthInfrastructure(
            configuration);

        services.AddPGLNAuthEntityFrameworkCore(
            configureDatabase);

        services.AddPGLNAuthAspNetCore(
            configuration);

        return services;
    }

    public static IServiceCollection AddPGLNAuthPostgreSql(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString,
        Action<PGLNAuthOptions>? configureAuth = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        return services.AddPGLNAuth(
            configuration,
            options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(
                            typeof(PostgreSqlAuthAssembly)
                                .Assembly
                                .GetName()
                                .Name);
                    });
            },
            configureAuth);
    }
}
