namespace PGLN.Auth.Application.Abstractions.Events;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
