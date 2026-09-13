using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Security;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Infrastructure.Authentication;
using PGLN.Auth.Infrastructure.Email;
using PGLN.Auth.Infrastructure.Events;
using PGLN.Auth.Infrastructure.Passwords;
using PGLN.Auth.Infrastructure.Time;

namespace PGLN.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configuration);

        var jwtOptions =
            configuration
                .GetSection("PGLNAuth:Jwt")
                .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration section 'PGLNAuth:Jwt' is missing.");

        jwtOptions.Validate();

        services.AddSingleton(
            jwtOptions);

        services
            .AddOptions<StepUpSecurityOptions>()
            .Bind(
                configuration.GetSection(
                    StepUpSecurityOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.HmacSecret),
                "Step-up HMAC secret is required.")
            .Validate(
                options =>
                    options.HmacSecret.Length >= 32,
                "Step-up HMAC secret must be at least 32 characters long.")
            .ValidateOnStart();

        services.AddDataProtection();

        services.AddSingleton<
            IIntegrationEventPayloadProtector,
            DataProtectionIntegrationEventPayloadProtector>();

        services.AddSingleton<
            IClock,
            SystemClock>();

        services.AddSingleton<
            IPasswordHasher,
            PasswordHasher>();

        services.AddSingleton<
            IVerificationTokenGenerator,
            SecureVerificationTokenGenerator>();

        services.AddSingleton<
            ITokenHasher,
            Sha256TokenHasher>();

        services.AddSingleton<
            IStepUpCodeGenerator,
            SecureStepUpCodeGenerator>();

        services.AddSingleton<
            IStepUpCodeProtector>(
            serviceProvider =>
            {
                var options =
                    serviceProvider
                        .GetRequiredService<
                            IOptions<StepUpSecurityOptions>>()
                        .Value;

                return new HmacStepUpCodeProtector(
                    options.HmacSecret);
            });

        services.AddSingleton<
            IAccessTokenGenerator,
            JwtAccessTokenGenerator>();

        services.AddSingleton<
            IRefreshTokenGenerator,
            SecureRefreshTokenGenerator>();

        services.AddSingleton<
            IPasswordResetTokenGenerator,
            SecurePasswordResetTokenGenerator>();

        services.AddSingleton<
            IEmailTemplateRenderer,
            DefaultEmailTemplateRenderer>();

        return services;
    }
}





