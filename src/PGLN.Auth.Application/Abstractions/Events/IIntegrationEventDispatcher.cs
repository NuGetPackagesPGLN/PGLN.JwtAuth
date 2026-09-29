namespace PGLN.Auth.Application.Abstractions.Events;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventDispatchContext context,
        CancellationToken cancellationToken = default);
}
