using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Application.Features.ForgotPassword;
using PGLN.Auth.Application.Features.Login;
using PGLN.Auth.Application.Messaging;

namespace PGLN.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthApplication(
        this IServiceCollection services,
        PasswordPolicyOptions? passwordPolicy = null,
        EmailVerificationOptions? emailVerificationOptions = null,
        EmailDeliveryOptions? emailDeliveryOptions = null,
        RefreshTokenOptions? refreshTokenOptions = null,
        AccountLockoutOptions? accountLockoutOptions = null,
        LoginEmailThrottleOptions? loginEmailThrottleOptions = null,
        StepUpChallengeOptions? stepUpChallengeOptions = null,
        PasswordResetOptions? passwordResetOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        passwordPolicy ??=
            new PasswordPolicyOptions();

        emailVerificationOptions ??=
            new EmailVerificationOptions();

        emailDeliveryOptions ??=
            new EmailDeliveryOptions();

        refreshTokenOptions ??=
            new RefreshTokenOptions();

        accountLockoutOptions ??=
            new AccountLockoutOptions();

        stepUpChallengeOptions ??=
            new StepUpChallengeOptions();

        passwordResetOptions ??=
            new PasswordResetOptions();

        passwordPolicy.Validate();
        emailVerificationOptions.Validate();
        emailDeliveryOptions.Validate();
        refreshTokenOptions.Validate();
        accountLockoutOptions.Validate();
        stepUpChallengeOptions.Validate();
        passwordResetOptions.Validate();

        services.AddSingleton(
            passwordPolicy);

        services.AddSingleton(
            emailVerificationOptions);

        services.AddSingleton(
            emailDeliveryOptions);

        services.AddSingleton(
            refreshTokenOptions);

        services.AddSingleton(
            accountLockoutOptions);

        services.AddSingleton(
            stepUpChallengeOptions);

        services.AddSingleton(
            passwordResetOptions);

        loginEmailThrottleOptions ??=
            new LoginEmailThrottleOptions();

        loginEmailThrottleOptions.Validate();

        services.AddSingleton(
            loginEmailThrottleOptions);

        services.AddValidatorsFromAssembly(
            typeof(ApplicationAssemblyReference).Assembly,
            includeInternalTypes: true);

        services.Scan(
            scan =>
                scan
                    .FromAssemblyOf<ApplicationAssemblyReference>()
                    .AddClasses(
                        classes =>
                            classes.AssignableTo(
                                typeof(ICommandHandler<,>)),
                        publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
                    .AddClasses(
                        classes =>
                            classes.AssignableTo(
                                typeof(IQueryHandler<,>)),
                        publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
                    .AddClasses(
                        classes =>
                            classes.AssignableTo(
                                typeof(IIntegrationEventHandler<>)))
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());

        services.Decorate(
            typeof(ICommandHandler<,>),
            typeof(ValidationCommandHandlerDecorator<,>));

        services.AddScoped<
            IRequestDispatcher,
            RequestDispatcher>();

        return services;
    }
}
