using PGLN.Auth.Application.Abstractions.Events;

namespace PGLN.Auth.Application.Tests.DependencyInjection;

internal sealed class StubIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        return Task.CompletedTask;
    }
}
