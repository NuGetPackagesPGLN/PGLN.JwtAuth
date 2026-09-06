using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public sealed class IntegrationEventDispatcher
    : IIntegrationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public IntegrationEventDispatcher(
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(
            serviceProvider);

        _serviceProvider =
            serviceProvider;
    }

    public async Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        var eventType =
            integrationEvent.GetType();

        var handlerInterface =
            typeof(IIntegrationEventHandler<>)
                .MakeGenericType(eventType);

        var handlers =
            _serviceProvider
                .GetServices(handlerInterface)
                .Where(handler => handler is not null)
                .Cast<object>()
                .ToArray();

        if (handlers.Length == 0)
        {
            throw new InvalidOperationException(
                $"No integration event handler is registered for '{eventType.FullName}'.");
        }

        var handleMethod =
            handlerInterface.GetMethod(
                "HandleAsync")
            ?? throw new InvalidOperationException(
                $"Unable to resolve HandleAsync for '{handlerInterface.FullName}'.");

        foreach (var handler in handlers)
        {
            var invocationResult =
                handleMethod.Invoke(
                    handler,
                    new object?[]
                    {
                        integrationEvent,
                        cancellationToken
                    });

            if (invocationResult is not Task task)
            {
                throw new InvalidOperationException(
                    $"Integration event handler '{handler.GetType().FullName}' did not return a Task.");
            }

            await task;
        }
    }
}
