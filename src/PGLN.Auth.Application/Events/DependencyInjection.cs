using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Dispatching;

namespace PGLN.Auth.Application.Events;

public static class DependencyInjection
{
    public static IServiceCollection AddPGLNAuthIntegrationEvents(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Scan(
            scan =>
                scan
                    .FromAssemblyOf<ApplicationAssemblyReference>()
                    .AddClasses(
                        classes =>
                            classes.AssignableTo(
                                typeof(IIntegrationEventHandler<>)))
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddScoped<
            IIntegrationEventDispatcher,
            IntegrationEventDispatcher>();

        return services;
    }
}
