using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;

namespace PGLN.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthApplication(
        this IServiceCollection services,
        PasswordPolicyOptions? passwordPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        passwordPolicy ??=
            new PasswordPolicyOptions();

        passwordPolicy.Validate();

        services.AddSingleton(passwordPolicy);

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
                    .WithTransientLifetime());

        services.Decorate(
            typeof(ICommandHandler<,>),
            typeof(ValidationCommandHandlerDecorator<,>));

        return services;
    }
}
