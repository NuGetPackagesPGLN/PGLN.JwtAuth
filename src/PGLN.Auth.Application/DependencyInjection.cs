using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Messaging;

namespace PGLN.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthApplication(
        this IServiceCollection services,
        PasswordPolicyOptions? passwordPolicy = null,
        EmailVerificationOptions? emailVerificationOptions = null,
        EmailDeliveryOptions? emailDeliveryOptions = null,
        RefreshTokenOptions? refreshTokenOptions = null)
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

        passwordPolicy.Validate();
        emailVerificationOptions.Validate();
        emailDeliveryOptions.Validate();
        refreshTokenOptions.Validate();

        services.AddSingleton(
            passwordPolicy);

        services.AddSingleton(
            emailVerificationOptions);

        services.AddSingleton(
            emailDeliveryOptions);

        services.AddSingleton(
            refreshTokenOptions);

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
                                typeof(ICommandHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
                    .AddClasses(
                        classes =>
                            classes.AssignableTo(
                                typeof(IQueryHandler<,>)))
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
