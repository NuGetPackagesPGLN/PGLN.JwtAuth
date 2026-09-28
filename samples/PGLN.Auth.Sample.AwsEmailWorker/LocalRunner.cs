using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Dispatching;

namespace PGLN.Auth.Sample.AwsEmailWorker;

internal static class LocalRunner
{
    public static void ValidateServices()
    {
        var provider =
            WorkerServices.Provider;

        using var scope =
            provider.CreateScope();

        _ =
            scope.ServiceProvider
                .GetRequiredService<IntegrationEventTypeRegistry>();

        _ =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventDispatcher>();

        Console.WriteLine(
            "AWS Email Worker dependency graph validated successfully.");
    }
}
