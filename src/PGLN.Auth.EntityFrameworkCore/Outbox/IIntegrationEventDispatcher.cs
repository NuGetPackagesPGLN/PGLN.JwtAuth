using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
